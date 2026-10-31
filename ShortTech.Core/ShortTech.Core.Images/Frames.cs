using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Drawing;

namespace ShortTech.Core.Images;

public static class Frames
{
	public static MagickImage Render(MagickImage photo, FrameOptions options, double scale = 1.0)
	{
		double num = options.Padding * scale;
		double num2 = (double)photo.Width + num * 2.0;
		double num3 = (double)photo.Height + num * 2.0;
		double? num4 = options.Ratio.Value();
		if (num4.HasValue)
		{
			double valueOrDefault = num4.GetValueOrDefault();
			if (num2 / num3 < valueOrDefault)
			{
				num2 = num3 * valueOrDefault;
			}
			else
			{
				num3 = num2 / valueOrDefault;
			}
		}
		uint num5 = (uint)Math.Round(num2);
		uint num6 = (uint)Math.Round(num3);
		if ((double)num5 * (double)num6 > 400000000.0)
		{
			throw new ShortTechException("This frame would be too large. Use less padding or a closer output ratio.");
		}
		MagickImage magickImage = Background(options.Background, num5, num6, photo);
		int num7 = (int)Math.Round((double)(num5 - photo.Width) / 2.0);
		int num8 = (int)Math.Round((double)(num6 - photo.Height) / 2.0);
		double radius = Math.Min(options.Corners * scale, (double)Math.Min(photo.Width, photo.Height) / 2.0);
		using MagickImage magickImage2 = Rounded(photo, radius);
		if (options.Shadow > 0.0)
		{
			double num9 = options.Shadow * scale;
			using MagickImage magickImage3 = (MagickImage)magickImage2.Clone();
			magickImage3.BackgroundColor = new MagickColor(0, 0, 0, 100);
			magickImage3.Shadow(0, 0, num9 / 2.0, new Percentage(40), MagickColors.Black);
			int num10 = (int)Math.Round(num9 * 0.35);
			magickImage.Composite(magickImage3, num7 - (int)Math.Round(num9), num8 - (int)Math.Round(num9) + num10, CompositeOperator.Over);
		}
		magickImage.Composite(magickImage2, num7, num8, CompositeOperator.Over);
		return magickImage;
	}

	public static MagickImage Rounded(MagickImage photo, double radius)
	{
		MagickImage magickImage = (MagickImage)photo.Clone();
		magickImage.Alpha(AlphaAction.Set);
		if (radius <= 0.5)
		{
			return magickImage;
		}
		using MagickImage image = new MagickImage(MagickColors.Transparent, magickImage.Width, magickImage.Height);
		new Drawables().FillColor(MagickColors.White).RoundRectangle(0.0, 0.0, magickImage.Width - 1, magickImage.Height - 1, radius, radius).Draw(image);
		magickImage.Composite(image, CompositeOperator.DstIn);
		return magickImage;
	}

	private static MagickImage Background(FrameBackground background, uint width, uint height, MagickImage photo)
	{
		if (!(background is FrameBackground.Solid { Color: var color }))
		{
			if (!(background is FrameBackground.Gradient gradient))
			{
				if (background is FrameBackground.Picture picture)
				{
					if (!File.Exists(picture.Path))
					{
						throw new ShortTechException("Choose a background image.");
					}
					return Fill(ImageEngine.Load(picture.Path, (int)Math.Max(width, height)), width, height);
				}
				MagickImage obj = (MagickImage)photo.Clone();
				obj.Resize(Math.Max(width, height) / 8, 0u);
				obj.Blur(0.0, (double)Math.Max(width, height) * 0.04 / 8.0);
				obj.Modulate(new Percentage(94), new Percentage(115));
				return Fill(obj, width, height);
			}
			Rgb[] array = FrameBackground.Gradients[gradient.Index % FrameBackground.Gradients.Length];
			MagickReadSettings magickReadSettings = new MagickReadSettings
			{
				Width = width,
				Height = height
			};
			magickReadSettings.SetDefine("gradient:angle", "45");
			return new MagickImage("gradient:" + array[0].Hex + "-" + array[1].Hex, magickReadSettings);
		}
		return new MagickImage(color.Magick, width, height);
	}

	public static MagickImage Fill(MagickImage image, uint width, uint height)
	{
		double num = Math.Max((double)width / (double)image.Width, (double)height / (double)image.Height);
		image.Resize((uint)Math.Ceiling((double)image.Width * num), (uint)Math.Ceiling((double)image.Height * num));
		image.Crop(new MagickGeometry((int)((image.Width - width) / 2), (int)((image.Height - height) / 2), width, height));
		image.ResetPage();
		return image;
	}

	public static async Task<IReadOnlyList<string>> Apply(IReadOnlyList<string> files, FrameOptions options, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Adding background {index + 1} of {files.Count}");
			context.Progress((double)index / (double)files.Count);
			string file = files[index];
			ImageFormat format = ImageOps.EditedFormat(file);
			string output = Output.PathFor(file, format.Ext, "framed");
			await Task.Run(delegate
			{
				using MagickImage photo = ImageEngine.Load(file);
				using MagickImage image = Render(photo, options);
				ImageEngine.Save(image, format, output, 0.92);
			});
			outputs.Add(output);
		}
		return outputs;
	}
}
