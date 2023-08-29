using System;
using System.Collections.Generic;
using Iodo.Assets.RtspControl.Entities;
using Iodo.Assets.RtspControl.Entities.Decoded;
using Iodo.Assets.RtspControl.Entities.FFmpeg;
using Iodo.Assets.RtspControl.Entities.Received;
using Iodo.Rtsp.RawFrames;
using Iodo.Rtsp.RawFrames.Audio;

namespace Iodo.Assets.RtspControl.Models;

public class RealtimeAudioSource : IAudioSource
{
	private IRawFramesSource _rawFramesSource;

	private readonly Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder> _audioDecodersMap = new Dictionary<FFmpegAudioCodecId, FFmpegAudioDecoder>();

	public event EventHandler<IDecodedAudioFrame> FrameReceived;

	public void SetRawFramesSource(IRawFramesSource rawFramesSource)
	{
		if (_rawFramesSource != null)
		{
			IRawFramesSource rawFramesSource2 = _rawFramesSource;
			rawFramesSource2.FrameReceived = (EventHandler<RawFrame>)Delegate.Remove(rawFramesSource2.FrameReceived, new EventHandler<RawFrame>(OnFrameReceived));
		}
		_rawFramesSource = rawFramesSource;
		if (rawFramesSource != null)
		{
			rawFramesSource.FrameReceived = (EventHandler<RawFrame>)Delegate.Combine(rawFramesSource.FrameReceived, new EventHandler<RawFrame>(OnFrameReceived));
		}
	}

	private void OnFrameReceived(object sender, RawFrame rawFrame)
	{
		if (rawFrame is RawAudioFrame rawAudioFrame)
		{
			FFmpegAudioDecoder decoderForFrame = GetDecoderForFrame(rawAudioFrame);
			if (decoderForFrame.TryDecode(rawAudioFrame))
			{
				IDecodedAudioFrame decodedFrame = decoderForFrame.GetDecodedFrame(new AudioConversionParameters
				{
					OutBitsPerSample = 16
				});
				this.FrameReceived?.Invoke(this, decodedFrame);
			}
		}
	}

	private FFmpegAudioDecoder GetDecoderForFrame(RawAudioFrame audioFrame)
	{
		FFmpegAudioCodecId fFmpegAudioCodecId = DetectCodecId(audioFrame);
		if (!_audioDecodersMap.TryGetValue(fFmpegAudioCodecId, out var value))
		{
			int bitsPerCodedSample = 0;
			if (audioFrame is RawG726Frame rawG726Frame)
			{
				bitsPerCodedSample = rawG726Frame.BitsPerCodedSample;
			}
			value = FFmpegAudioDecoder.CreateDecoder(fFmpegAudioCodecId, bitsPerCodedSample);
			_audioDecodersMap.Add(fFmpegAudioCodecId, value);
		}
		return value;
	}

	private FFmpegAudioCodecId DetectCodecId(RawAudioFrame audioFrame)
	{
		if (audioFrame is RawAACFrame)
		{
			return FFmpegAudioCodecId.AAC;
		}
		if (audioFrame is RawG711AFrame)
		{
			return FFmpegAudioCodecId.G711A;
		}
		if (audioFrame is RawG711UFrame)
		{
			return FFmpegAudioCodecId.G711U;
		}
		if (audioFrame is RawG726Frame)
		{
			return FFmpegAudioCodecId.G726;
		}
		throw new ArgumentOutOfRangeException("audioFrame");
	}
}
