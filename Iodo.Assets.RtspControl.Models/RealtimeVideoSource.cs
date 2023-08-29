using System;
using System.Collections.Generic;
using Iodo.Assets.RtspControl.Entities.Decoded;
using Iodo.Assets.RtspControl.Entities.FFmpeg;
using Iodo.Assets.RtspControl.Entities.Received;
using Iodo.Rtsp.RawFrames;
using Iodo.Rtsp.RawFrames.Video;

namespace Iodo.Assets.RtspControl.Models;

public class RealtimeVideoSource : IVideoSource, IDisposable
{
	private IRawFramesSource _rawFramesSource;

	private readonly Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder> _videoDecodersMap = new Dictionary<FFmpegVideoCodecId, FFmpegVideoDecoder>();

	public event EventHandler<IDecodedVideoFrame> FrameReceived;

	public void SetRawFramesSource(IRawFramesSource rawFramesSource)
	{
		if (_rawFramesSource != null)
		{
			IRawFramesSource rawFramesSource2 = _rawFramesSource;
			rawFramesSource2.FrameReceived = (EventHandler<RawFrame>)Delegate.Remove(rawFramesSource2.FrameReceived, new EventHandler<RawFrame>(OnFrameReceived));
			DropAllVideoDecoders();
		}
		_rawFramesSource = rawFramesSource;
		if (rawFramesSource != null)
		{
			rawFramesSource.FrameReceived = (EventHandler<RawFrame>)Delegate.Combine(rawFramesSource.FrameReceived, new EventHandler<RawFrame>(OnFrameReceived));
		}
	}

	public void Dispose()
	{
		DropAllVideoDecoders();
	}

	private void DropAllVideoDecoders()
	{
		foreach (FFmpegVideoDecoder value in _videoDecodersMap.Values)
		{
			value.Dispose();
		}
		_videoDecodersMap.Clear();
	}

	private void OnFrameReceived(object sender, RawFrame rawFrame)
	{
		if (rawFrame is RawVideoFrame rawVideoFrame)
		{
			FFmpegVideoDecoder decoderForFrame = GetDecoderForFrame(rawVideoFrame);
			IDecodedVideoFrame decodedVideoFrame = decoderForFrame.TryDecode(rawVideoFrame);
			if (decodedVideoFrame != null)
			{
				this.FrameReceived?.Invoke(this, decodedVideoFrame);
			}
		}
	}

	private FFmpegVideoDecoder GetDecoderForFrame(RawVideoFrame videoFrame)
	{
		FFmpegVideoCodecId fFmpegVideoCodecId = DetectCodecId(videoFrame);
		if (!_videoDecodersMap.TryGetValue(fFmpegVideoCodecId, out var value))
		{
			value = FFmpegVideoDecoder.CreateDecoder(fFmpegVideoCodecId);
			_videoDecodersMap.Add(fFmpegVideoCodecId, value);
		}
		return value;
	}

	private FFmpegVideoCodecId DetectCodecId(RawVideoFrame videoFrame)
	{
		if (videoFrame is RawJpegFrame)
		{
			return FFmpegVideoCodecId.MJPEG;
		}
		if (videoFrame is RawH264Frame)
		{
			return FFmpegVideoCodecId.H264;
		}
		throw new ArgumentOutOfRangeException("videoFrame");
	}
}
