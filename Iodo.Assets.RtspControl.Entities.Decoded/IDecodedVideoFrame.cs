using System;

namespace Iodo.Assets.RtspControl.Entities.Decoded;

public interface IDecodedVideoFrame
{
	void TransformTo(IntPtr buffer, int bufferStride, TransformParameters transformParameters);
}
