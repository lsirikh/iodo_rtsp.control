using System.Reflection;
using System.Windows;

namespace Iodo.Assets.RtspControl.Models;

public static class ScreenInfo
{
	public static readonly double DpiX;

	public static readonly double DpiY;

	static ScreenInfo()
	{
		PropertyInfo property = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.Static | BindingFlags.NonPublic);
		PropertyInfo property2 = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.Static | BindingFlags.NonPublic);
		DpiX = ((property != null) ? ((double)(int)property.GetValue(null, null)) : 96.0);
		DpiY = ((property2 != null) ? ((double)(int)property2.GetValue(null, null)) : 96.0);
	}
}
