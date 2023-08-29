using Iodo.Assets.RtspControl.Entities.FFmpeg;

namespace Iodo.Assets.RtspControl.Entities;

internal class DecodedVideoFrameParameters
{
	public int Width { get; }

	public int Height { get; }

	public FFmpegPixelFormat PixelFormat { get; }

	public DecodedVideoFrameParameters(int width, int height, FFmpegPixelFormat pixelFormat)
	{
		Width = width;
		Height = height;
		PixelFormat = pixelFormat;
	}

	protected bool Equals(DecodedVideoFrameParameters other)
	{
		return Width == other.Width && Height == other.Height && PixelFormat == other.PixelFormat;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((DecodedVideoFrameParameters)obj);
	}

	public override int GetHashCode()
	{
		int width = Width;
		width = (width * 397) ^ Height;
		return (width * 397) ^ (int)PixelFormat;
	}
}
