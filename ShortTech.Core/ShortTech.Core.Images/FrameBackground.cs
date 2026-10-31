using System.Runtime.CompilerServices;

namespace ShortTech.Core.Images;

public abstract record FrameBackground
{
	public sealed record Gradient(int Index) : FrameBackground();

	public sealed record Solid(Rgb Color) : FrameBackground();

	public sealed record Picture(string Path) : FrameBackground();

	public sealed record Blurred : FrameBackground;

	public static readonly Rgb[][] Gradients = new Rgb[9][]
	{
		new Rgb[2]
		{
			new Rgb(0.96, 0.45, 0.33),
			new Rgb(0.98, 0.78, 0.33)
		},
		new Rgb[2]
		{
			new Rgb(0.98, 0.62, 0.55),
			new Rgb(0.6, 0.45, 0.92)
		},
		new Rgb[2]
		{
			new Rgb(1.0, 0.84, 0.4),
			new Rgb(0.95, 0.42, 0.62)
		},
		new Rgb[2]
		{
			new Rgb(0.98, 0.85, 0.88),
			new Rgb(0.82, 0.8, 0.98)
		},
		new Rgb[2]
		{
			new Rgb(0.8, 0.62, 0.92),
			new Rgb(0.98, 0.72, 0.8)
		},
		new Rgb[2]
		{
			new Rgb(0.13, 0.33, 0.42),
			new Rgb(0.95, 0.6, 0.36)
		},
		new Rgb[2]
		{
			new Rgb(0.35, 0.65, 0.98),
			new Rgb(0.6, 0.9, 0.95)
		},
		new Rgb[2]
		{
			new Rgb(0.45, 0.85, 0.82),
			new Rgb(0.25, 0.5, 0.92)
		},
		new Rgb[2]
		{
			new Rgb(0.92, 0.94, 1.0),
			new Rgb(0.68, 0.74, 0.98)
		}
	};

	public static readonly Rgb[] Solids = new Rgb[9]
	{
		new Rgb(1.0, 1.0, 1.0),
		new Rgb(0.93, 0.78, 0.38),
		new Rgb(0.68, 0.8, 0.36),
		new Rgb(0.38, 0.62, 0.44),
		new Rgb(0.36, 0.6, 0.6),
		new Rgb(0.5, 0.78, 0.9),
		new Rgb(0.27, 0.4, 0.82),
		new Rgb(0.16, 0.2, 0.3),
		new Rgb(0.42, 0.3, 0.68)
	};

	[CompilerGenerated]
	protected FrameBackground(FrameBackground original)
	{
	}

	protected FrameBackground()
	{
	}
}
