using System;

namespace Iodo.Assets.RtspControl.Entities.FFmpeg;

internal class FFmpegDecodedVideoScaler
{
	private const double MaxAspectRatioError = 0.1;

	private bool _disposed;

	public IntPtr Handle { get; }

	public int ScaledWidth { get; }

	public int ScaledHeight { get; }

	public PixelFormat ScaledPixelFormat { get; }

	private FFmpegDecodedVideoScaler(IntPtr handle, int scaledWidth, int scaledHeight, PixelFormat scaledPixelFormat)
	{
		Handle = handle;
		ScaledWidth = scaledWidth;
		ScaledHeight = scaledHeight;
		ScaledPixelFormat = scaledPixelFormat;
	}

	~FFmpegDecodedVideoScaler()
	{
		Dispose();
	}

	public static FFmpegDecodedVideoScaler Create(DecodedVideoFrameParameters decodedVideoFrameParameters, TransformParameters transformParameters)
	{
		if (decodedVideoFrameParameters == null)
		{
			throw new ArgumentNullException("decodedVideoFrameParameters");
		}
		if (transformParameters == null)
		{
			throw new ArgumentNullException("transformParameters");
		}
		int sourceLeft = 0;
		int sourceTop = 0;
		int num = decodedVideoFrameParameters.Width;
		int num2 = decodedVideoFrameParameters.Height;
		int num3 = decodedVideoFrameParameters.Width;
		int num4 = decodedVideoFrameParameters.Height;
		if (!transformParameters.RegionOfInterest.IsEmpty)
		{
			sourceLeft = (int)((float)decodedVideoFrameParameters.Width * transformParameters.RegionOfInterest.Left);
			sourceTop = (int)((float)decodedVideoFrameParameters.Height * transformParameters.RegionOfInterest.Top);
			num = (int)((float)decodedVideoFrameParameters.Width * transformParameters.RegionOfInterest.Width);
			num2 = (int)((float)decodedVideoFrameParameters.Height * transformParameters.RegionOfInterest.Height);
		}
		if (!transformParameters.TargetFrameSize.IsEmpty)
		{
			num3 = transformParameters.TargetFrameSize.Width;
			num4 = transformParameters.TargetFrameSize.Height;
			ScalingPolicy scalingPolicy = transformParameters.ScalePolicy;
			float num5 = (float)num / (float)num2;
			float num6 = (float)num3 / (float)num4;
			if (scalingPolicy == ScalingPolicy.Auto)
			{
				float num7 = Math.Abs(num5 - num6) / num5;
				scalingPolicy = ((!((double)num7 > 0.1)) ? ScalingPolicy.Stretch : ScalingPolicy.RespectAspectRatio);
			}
			if (scalingPolicy == ScalingPolicy.RespectAspectRatio)
			{
				if (num6 < num5)
				{
					num4 = num2 * num3 / num;
				}
				else
				{
					num3 = num * num4 / num2;
				}
			}
		}
		PixelFormat targetFormat = transformParameters.TargetFormat;
		FFmpegPixelFormat fFmpegPixelFormat = GetFFmpegPixelFormat(targetFormat);
		FFmpegScalingQuality fFmpegScaleQuality = GetFFmpegScaleQuality(transformParameters.ScaleQuality);
		if (FFmpegVideoPInvoke.CreateVideoScaler(sourceLeft, sourceTop, num, num2, decodedVideoFrameParameters.PixelFormat, num3, num4, fFmpegPixelFormat, fFmpegScaleQuality, out var handle) != 0)
		{
			throw new DecoderException("An error occurred while creating scaler, code: {resultCode}");
		}
		return new FFmpegDecodedVideoScaler(handle, num3, num4, targetFormat);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			FFmpegVideoPInvoke.RemoveVideoScaler(Handle);
			GC.SuppressFinalize(this);
		}
	}

	private static FFmpegScalingQuality GetFFmpegScaleQuality(ScalingQuality scalingQuality)
	{
		return scalingQuality switch
		{
			ScalingQuality.Nearest => FFmpegScalingQuality.Point, 
			ScalingQuality.Bilinear => FFmpegScalingQuality.Bilinear, 
			ScalingQuality.FastBilinear => FFmpegScalingQuality.FastBilinear, 
			ScalingQuality.Bicubic => FFmpegScalingQuality.Bicubic, 
			_ => throw new ArgumentOutOfRangeException("scalingQuality"), 
		};
	}

	private static FFmpegPixelFormat GetFFmpegPixelFormat(PixelFormat pixelFormat)
	{
		return pixelFormat switch
		{
			PixelFormat.Bgra32 => FFmpegPixelFormat.BGRA, 
			PixelFormat.Grayscale => FFmpegPixelFormat.GRAY8, 
			PixelFormat.Bgr24 => FFmpegPixelFormat.BGR24, 
			_ => throw new ArgumentOutOfRangeException("pixelFormat"), 
		};
	}
}
