using System;
using ImageMagick;

namespace ShortTech.Core.Images;

public readonly record struct Rgb(double R, double G, double B)
{
	public MagickColor Magick => new MagickColor((byte)Math.Round(R * 255.0), (byte)Math.Round(G * 255.0), (byte)Math.Round(B * 255.0));

	public string Hex => $"#{(int)Math.Round(R * 255.0):X2}{(int)Math.Round(G * 255.0):X2}{(int)Math.Round(B * 255.0):X2}";
}
