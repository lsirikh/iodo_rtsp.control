using System.Drawing;

namespace Iodo.Assets.RtspControl.Entities;

public class TransformParameters
{
	public RectangleF RegionOfInterest { get; }

	public Size TargetFrameSize { get; }

	public ScalingPolicy ScalePolicy { get; }

	public PixelFormat TargetFormat { get; }

	public ScalingQuality ScaleQuality { get; }

	public TransformParameters(RectangleF regionOfInterest, Size targetFrameSize, ScalingPolicy scalePolicy, PixelFormat targetFormat, ScalingQuality scaleQuality)
	{
		RegionOfInterest = regionOfInterest;
		TargetFrameSize = targetFrameSize;
		TargetFormat = targetFormat;
		ScaleQuality = scaleQuality;
		ScalePolicy = scalePolicy;
	}

	protected bool Equals(TransformParameters other)
	{
		return RegionOfInterest.Equals(other.RegionOfInterest) && TargetFrameSize.Equals(other.TargetFrameSize) && TargetFormat == other.TargetFormat && ScaleQuality == other.ScaleQuality;
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
		return Equals((TransformParameters)obj);
	}

	public override int GetHashCode()
	{
		int hashCode = RegionOfInterest.GetHashCode();
		hashCode = (hashCode * 397) ^ TargetFrameSize.GetHashCode();
		hashCode = (hashCode * 397) ^ (int)TargetFormat;
		return (hashCode * 397) ^ (int)ScaleQuality;
	}
}
