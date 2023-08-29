#define DEBUG
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Iodo.Assets.RtspControl.Entities.Decoded;
using Iodo.Assets.RtspControl.Entities.Decoded.Frames;
using Iodo.Rtsp.RawFrames.Audio;

namespace Iodo.Assets.RtspControl.Entities.FFmpeg;

internal class FFmpegAudioDecoder
{
	private readonly IntPtr _decoderHandle;

	private readonly FFmpegAudioCodecId _audioCodecId;

	private IntPtr _resamplerHandle;

	private AudioFrameFormat _currentFrameFormat = new AudioFrameFormat(0, 0, 0);

	private DateTime _currentRawFrameTimestamp;

	private byte[] _extraData = new byte[0];

	private byte[] _decodedFrameBuffer = new byte[0];

	private bool _disposed;

	public int BitsPerCodedSample { get; }

	private FFmpegAudioDecoder(FFmpegAudioCodecId audioCodecId, int bitsPerCodedSample, IntPtr decoderHandle)
	{
		_audioCodecId = audioCodecId;
		BitsPerCodedSample = bitsPerCodedSample;
		_decoderHandle = decoderHandle;
	}

	~FFmpegAudioDecoder()
	{
		Dispose();
	}

	public static FFmpegAudioDecoder CreateDecoder(FFmpegAudioCodecId audioCodecId, int bitsPerCodedSample)
	{
		IntPtr handle;
		int num = FFmpegAudioPInvoke.CreateAudioDecoder(audioCodecId, bitsPerCodedSample, out handle);
		if (num != 0)
		{
			throw new DecoderException($"An error occurred while creating audio decoder for {audioCodecId} codec, code: {num}");
		}
		return new FFmpegAudioDecoder(audioCodecId, bitsPerCodedSample, handle);
	}

	public unsafe bool TryDecode(RawAudioFrame rawAudioFrame)
	{
		if (rawAudioFrame is RawAACFrame rawAACFrame)
		{
			Debug.Assert(rawAACFrame.ConfigSegment.Array != null, "aacFrame.ConfigSegment.Array != null");
			if (!_extraData.SequenceEqual(rawAACFrame.ConfigSegment))
			{
				if (_extraData.Length == rawAACFrame.ConfigSegment.Count)
				{
					Buffer.BlockCopy(rawAACFrame.ConfigSegment.Array, rawAACFrame.ConfigSegment.Offset, _extraData, 0, rawAACFrame.ConfigSegment.Count);
				}
				else
				{
					_extraData = rawAACFrame.ConfigSegment.ToArray();
				}
				fixed (byte* ptr = &_extraData[0])
				{
					int num = FFmpegAudioPInvoke.SetAudioDecoderExtraData(_decoderHandle, (IntPtr)ptr, rawAACFrame.ConfigSegment.Count);
					if (num != 0)
					{
						throw new DecoderException($"An error occurred while setting audio extra data, {_audioCodecId} codec, code: {num}");
					}
				}
			}
		}
		Debug.Assert(rawAudioFrame.FrameSegment.Array != null, "rawAudioFrame.FrameSegment.Array != null");
		fixed (byte* ptr2 = &rawAudioFrame.FrameSegment.Array[rawAudioFrame.FrameSegment.Offset])
		{
			int sampleRate;
			int bitsPerSample;
			int channels;
			int num2 = FFmpegAudioPInvoke.DecodeFrame(_decoderHandle, (IntPtr)ptr2, rawAudioFrame.FrameSegment.Count, out sampleRate, out bitsPerSample, out channels);
			_currentRawFrameTimestamp = rawAudioFrame.Timestamp;
			if (num2 != 0)
			{
				return false;
			}
			if (rawAudioFrame is RawG711Frame rawG711Frame)
			{
				sampleRate = rawG711Frame.SampleRate;
				channels = rawG711Frame.Channels;
			}
			if (_currentFrameFormat.SampleRate != sampleRate || _currentFrameFormat.BitPerSample != bitsPerSample || _currentFrameFormat.Channels != channels)
			{
				_currentFrameFormat = new AudioFrameFormat(sampleRate, bitsPerSample, channels);
				if (_resamplerHandle != IntPtr.Zero)
				{
					FFmpegAudioPInvoke.RemoveAudioResampler(_resamplerHandle);
				}
			}
		}
		return true;
	}

	public IDecodedAudioFrame GetDecodedFrame(AudioConversionParameters optionalAudioConversionParameters = null)
	{
		IntPtr outBuffer;
		int outDataSize;
		AudioFrameFormat format;
		if (optionalAudioConversionParameters == null || ((optionalAudioConversionParameters.OutSampleRate == 0 || optionalAudioConversionParameters.OutSampleRate == _currentFrameFormat.SampleRate) && (optionalAudioConversionParameters.OutBitsPerSample == 0 || optionalAudioConversionParameters.OutBitsPerSample == _currentFrameFormat.BitPerSample) && (optionalAudioConversionParameters.OutChannels == 0 || optionalAudioConversionParameters.OutChannels == _currentFrameFormat.Channels)))
		{
			int decodedFrame = FFmpegAudioPInvoke.GetDecodedFrame(_decoderHandle, out outBuffer, out outDataSize);
			if (decodedFrame != 0)
			{
				throw new DecoderException($"An error occurred while getting decoded audio frame, {_audioCodecId} codec, code: {decodedFrame}");
			}
			format = _currentFrameFormat;
		}
		else
		{
			int decodedFrame;
			if (_resamplerHandle == IntPtr.Zero)
			{
				decodedFrame = FFmpegAudioPInvoke.CreateAudioResampler(_decoderHandle, optionalAudioConversionParameters.OutSampleRate, optionalAudioConversionParameters.OutBitsPerSample, optionalAudioConversionParameters.OutChannels, out _resamplerHandle);
				if (decodedFrame != 0)
				{
					throw new DecoderException($"An error occurred while creating audio resampler, code: {decodedFrame}");
				}
			}
			decodedFrame = FFmpegAudioPInvoke.ResampleDecodedFrame(_decoderHandle, _resamplerHandle, out outBuffer, out outDataSize);
			if (decodedFrame != 0)
			{
				throw new DecoderException($"An error occurred while converting audio frame, code: {decodedFrame}");
			}
			format = new AudioFrameFormat((optionalAudioConversionParameters.OutSampleRate != 0) ? optionalAudioConversionParameters.OutSampleRate : _currentFrameFormat.SampleRate, (optionalAudioConversionParameters.OutBitsPerSample != 0) ? optionalAudioConversionParameters.OutBitsPerSample : _currentFrameFormat.BitPerSample, (optionalAudioConversionParameters.OutChannels != 0) ? optionalAudioConversionParameters.OutChannels : _currentFrameFormat.Channels);
		}
		if (_decodedFrameBuffer.Length < outDataSize)
		{
			_decodedFrameBuffer = new byte[outDataSize];
		}
		Marshal.Copy(outBuffer, _decodedFrameBuffer, 0, outDataSize);
		return new DecodedAudioFrame(_currentRawFrameTimestamp, new ArraySegment<byte>(_decodedFrameBuffer, 0, outDataSize), format);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			FFmpegAudioPInvoke.RemoveAudioDecoder(_decoderHandle);
			if (_resamplerHandle != IntPtr.Zero)
			{
				FFmpegAudioPInvoke.RemoveAudioResampler(_resamplerHandle);
			}
			GC.SuppressFinalize(this);
		}
	}
}
