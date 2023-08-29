using System;
using System.Collections.Generic;
using System.Linq;
using Iodo.Assets.RtspControl.Entities.Decoded;
using Iodo.Assets.RtspControl.Entities.Decoded.Frames;
using Iodo.Rtsp.RawFrames.Video;

namespace Iodo.Assets.RtspControl.Entities.FFmpeg;

internal class FFmpegVideoDecoder
{
	private readonly IntPtr _decoderHandle;

	private readonly FFmpegVideoCodecId _videoCodecId;

	private DecodedVideoFrameParameters _currentFrameParameters = new DecodedVideoFrameParameters(0, 0, FFmpegPixelFormat.None);

	private readonly Dictionary<TransformParameters, FFmpegDecodedVideoScaler> _scalersMap = new Dictionary<TransformParameters, FFmpegDecodedVideoScaler>();

	private byte[] _extraData = new byte[0];

	private bool _disposed;

	private FFmpegVideoDecoder(FFmpegVideoCodecId videoCodecId, IntPtr decoderHandle)
	{
		_videoCodecId = videoCodecId;
		_decoderHandle = decoderHandle;
	}

	~FFmpegVideoDecoder()
	{
		Dispose();
	}

	public static FFmpegVideoDecoder CreateDecoder(FFmpegVideoCodecId videoCodecId)
	{
		IntPtr handle;
		int num = FFmpegVideoPInvoke.CreateVideoDecoder(videoCodecId, out handle);
		if (num != 0)
		{
			throw new DecoderException($"An error occurred while creating video decoder for {videoCodecId} codec, code: {num}");
		}
		return new FFmpegVideoDecoder(videoCodecId, handle);
	}

	public unsafe IDecodedVideoFrame TryDecode(RawVideoFrame rawVideoFrame)
	{
		fixed (byte* ptr2 = &rawVideoFrame.FrameSegment.Array[rawVideoFrame.FrameSegment.Offset])
		{
			if (rawVideoFrame is RawH264IFrame rawH264IFrame && rawH264IFrame.SpsPpsSegment.Array != null && !_extraData.SequenceEqual(rawH264IFrame.SpsPpsSegment))
			{
				if (_extraData.Length != rawH264IFrame.SpsPpsSegment.Count)
				{
					_extraData = new byte[rawH264IFrame.SpsPpsSegment.Count];
				}
				Buffer.BlockCopy(rawH264IFrame.SpsPpsSegment.Array, rawH264IFrame.SpsPpsSegment.Offset, _extraData, 0, rawH264IFrame.SpsPpsSegment.Count);
				fixed (byte* ptr = &_extraData[0])
				{
					int num = FFmpegVideoPInvoke.SetVideoDecoderExtraData(_decoderHandle, (IntPtr)ptr, _extraData.Length);
					if (num != 0)
					{
						throw new DecoderException($"An error occurred while setting video extra data, {_videoCodecId} codec, code: {num}");
					}
				}
			}
			if (FFmpegVideoPInvoke.DecodeFrame(_decoderHandle, (IntPtr)ptr2, rawVideoFrame.FrameSegment.Count, out var frameWidth, out var frameHeight, out var framePixelFormat) != 0)
			{
				return null;
			}
			if (_currentFrameParameters.Width != frameWidth || _currentFrameParameters.Height != frameHeight || _currentFrameParameters.PixelFormat != framePixelFormat)
			{
				_currentFrameParameters = new DecodedVideoFrameParameters(frameWidth, frameHeight, framePixelFormat);
				DropAllVideoScalers();
			}
			return new DecodedVideoFrame(TransformTo);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			FFmpegVideoPInvoke.RemoveVideoDecoder(_decoderHandle);
			DropAllVideoScalers();
			GC.SuppressFinalize(this);
		}
	}

	private void DropAllVideoScalers()
	{
		foreach (FFmpegDecodedVideoScaler value in _scalersMap.Values)
		{
			value.Dispose();
		}
		_scalersMap.Clear();
	}

	private void TransformTo(IntPtr buffer, int bufferStride, TransformParameters parameters)
	{
		if (!_scalersMap.TryGetValue(parameters, out var value))
		{
			value = FFmpegDecodedVideoScaler.Create(_currentFrameParameters, parameters);
			_scalersMap.Add(parameters, value);
		}
		int num = FFmpegVideoPInvoke.ScaleDecodedVideoFrame(_decoderHandle, value.Handle, buffer, bufferStride);
		if (num != 0)
		{
			throw new DecoderException($"An error occurred while converting decoding video frame, {_videoCodecId} codec, code: {num}");
		}
	}
}
