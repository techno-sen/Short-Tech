using System.Windows;
using System.Windows.Media;
using ShortTech.Core;

namespace ShortTech.App;

public static class Theme
{
	public static readonly Color AccentColor = Color.FromRgb(91, 71, 230);

	public static readonly Brush Accent = Freeze(new SolidColorBrush(AccentColor));

	public static readonly Brush AccentSoft = Freeze(new SolidColorBrush(Color.FromArgb(38, 91, 71, 230)));

	public static readonly Brush Mint = Freeze(new SolidColorBrush(Color.FromRgb(124, 242, 200)));

	public static readonly Brush Ink = Freeze(new SolidColorBrush(Color.FromRgb(40, 43, 41)));

	public static readonly Brush Muted = Freeze(new SolidColorBrush(Color.FromRgb(117, 117, 117)));

	public static readonly Brush Surface = Freeze(new SolidColorBrush(Color.FromRgb(250, 250, 251)));

	public static readonly Brush Card = Freeze(new SolidColorBrush(Color.FromArgb(11, 0, 0, 0)));

	public static readonly Brush Hairline = Freeze(new SolidColorBrush(Color.FromArgb(26, 0, 0, 0)));

	public static readonly Brush ButtonFill = Freeze(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)));

	public static readonly Brush ButtonPressed = Freeze(new SolidColorBrush(Color.FromArgb(36, 0, 0, 0)));

	public static readonly Brush PetalFill = Freeze(new SolidColorBrush(Color.FromArgb(240, 248, 249, 250)));

	public static readonly Brush PetalStroke = Freeze(new SolidColorBrush(Color.FromArgb(56, 93, 102, 102)));

	public static readonly Brush Disc = Freeze(new SolidColorBrush(Color.FromArgb(184, 238, 240, 242)));

	public static readonly FontFamily Font = new FontFamily("Segoe UI Variable Text, Segoe UI, Tahoma");

	public static readonly FontFamily Display = new FontFamily("Segoe UI Variable Display, Segoe UI, Tahoma");

	public static readonly FontFamily Icons = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

	public static FlowDirection Direction
	{
		get
		{
			if (!L10n.IsRightToLeft)
			{
				return FlowDirection.LeftToRight;
			}
			return FlowDirection.RightToLeft;
		}
	}

	private static Brush Freeze(Brush brush)
	{
		brush.Freeze();
		return brush;
	}
}
