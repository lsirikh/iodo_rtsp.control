using System;
using Iodo.Assets.RtspControl.Entities.Decoded;

namespace Iodo.Assets.RtspControl.Models;

public interface IAudioSource
{
	event EventHandler<IDecodedAudioFrame> FrameReceived;
}
