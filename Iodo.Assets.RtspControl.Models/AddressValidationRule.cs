using System;
using System.Globalization;
using System.Windows.Controls;

namespace Iodo.Assets.RtspControl.Models;

public class AddressValidationRule : ValidationRule
{
	private const string RtspPrefix = "rtsp://";

	public override ValidationResult Validate(object value, CultureInfo cultureInfo)
	{
		string text = value as string;
		if (string.IsNullOrEmpty(text))
		{
			return new ValidationResult(isValid: false, "Invalid address");
		}
		if (!text.StartsWith("rtsp://"))
		{
			text = "rtsp://" + text;
		}
		if (!Uri.TryCreate(text, UriKind.Absolute, out var _))
		{
			return new ValidationResult(isValid: false, "Invalid address");
		}
		return new ValidationResult(isValid: true, null);
	}
}
