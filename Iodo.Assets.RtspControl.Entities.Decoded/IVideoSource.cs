using System;

namespace Iodo.Assets.RtspControl.Entities.Decoded;

public interface IVideoSource
{
	event EventHandler<IDecodedVideoFrame> ReceivedFrame;
}
