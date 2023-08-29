using System;

namespace Iodo.Assets.RtspControl.Entities.Decoded;

public interface IDecodedAudioFrame
{
	DateTime Timestamp { get; }

	ArraySegment<byte> DecodedBytes { get; }

	AudioFrameFormat Format { get; }
}
