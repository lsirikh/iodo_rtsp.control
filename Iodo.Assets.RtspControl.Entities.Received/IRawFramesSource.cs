using System;
using Iodo.Rtsp.RawFrames;

namespace Iodo.Assets.RtspControl.Entities.Received;

public interface IRawFramesSource
{
	EventHandler<RawFrame> FrameReceived { get; set; }

	EventHandler<string> ConnectionStatusChanged { get; set; }

	void Start();

	void Stop();
}
