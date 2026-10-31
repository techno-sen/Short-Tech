using System.Collections.Generic;
using System.Windows;
using ShortTech.Core;

namespace ShortTech.App.Wheel;

public static class WheelPreview
{
	public static FrameworkElement Make(WheelMode mode, IReadOnlyList<string> files, int? hovered)
	{
		return new WheelWindow().Preview(mode, files, hovered);
	}
}
