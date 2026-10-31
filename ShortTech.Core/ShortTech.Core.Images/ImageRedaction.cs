using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Drawing;

namespace ShortTech.Core.Images;

public static class ImageRedaction
{
	public sealed record Box(double X, double Y, double W, double H);

	public static void Apply(MagickImage image, IEnumerable<Box> boxes, bool blur, Rgb color)
	{
		foreach (Box box in boxes)
		{
			MagickGeometry magickGeometry = new MagickGeometry((int)Math.Round(box.X * (double)image.Width), (int)Math.Round(box.Y * (double)image.Height), (uint)Math.Max(1.0, Math.Round(box.W * (double)image.Width)), (uint)Math.Max(1.0, Math.Round(box.H * (double)image.Height)));
			if (blur)
			{
				using MagickImage magickImage = (MagickImage)image.Clone();
				magickImage.Crop(magickGeometry);
				magickImage.ResetPage();
				magickImage.Scale(Math.Max(2u, magickImage.Width / 16), Math.Max(2u, magickImage.Height / 16));
				magickImage.Scale(magickGeometry.Width, magickGeometry.Height);
				magickImage.Blur(0.0, Math.Max(2.0, (double)Math.Min(magickGeometry.Width, magickGeometry.Height) / 12.0));
				image.Composite(magickImage, magickGeometry.X, magickGeometry.Y, CompositeOperator.Over);
			}
			else
			{
				new Drawables().FillColor(color.Magick).Rectangle(magickGeometry.X, magickGeometry.Y, magickGeometry.X + magickGeometry.Width, magickGeometry.Y + magickGeometry.Height).Draw(image);
			}
		}
	}

	public static async Task<string> Save(string file, IReadOnlyList<Box> boxes, bool blur, Rgb color, OperationContext context)
	{
		context.Report("Saving redacted copy");
		ImageFormat format = ImageOps.EditedFormat(file);
		string output = Output.PathFor(file, format.Ext, "redacted");
		await Task.Run(delegate
		{
			using MagickImage image = ImageEngine.Load(file);
			ApplyAndStrip(image, boxes, blur, color);
			ImageEngine.Save(image, format, output, 0.92, keepMetadata: false);
		});
		return output;
	}

	private static void ApplyAndStrip(MagickImage image, IReadOnlyList<Box> boxes, bool blur, Rgb color)
	{
		Apply(image, boxes, blur, color);
		image.RemoveProfile("exif");
		image.RemoveProfile("xmp");
		image.RemoveProfile("iptc");
	}
}
