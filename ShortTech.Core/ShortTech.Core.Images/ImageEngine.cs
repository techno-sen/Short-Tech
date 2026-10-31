using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;

namespace ShortTech.Core.Images;

public static class ImageEngine
{
	static ImageEngine()
	{
		ResourceLimits.Memory = 2147483648uL;
	}

	public static MagickImage Load(string path, int? maxPixels = null)
	{
		MagickReadSettings magickReadSettings = new MagickReadSettings();
		if (FileKinds.Ext(path) == "svg")
		{
			magickReadSettings.Density = new Density(192.0);
		}
		MagickImage magickImage = new MagickImage(path, magickReadSettings);
		magickImage.AutoOrient();
		if (maxPixels.HasValue)
		{
			int valueOrDefault = maxPixels.GetValueOrDefault();
			if (Math.Max(magickImage.Width, magickImage.Height) > valueOrDefault)
			{
				magickImage.Resize(new MagickGeometry((uint)valueOrDefault, (uint)valueOrDefault));
			}
		}
		return magickImage;
	}

	public static async Task<MagickImage> LoadAsync(string path, int? maxPixels = null)
	{
		return await Task.Run(() => Load(path, maxPixels));
	}

	public static byte[] Encode(MagickImage image, ImageFormat format, double quality, bool keepMetadata = true)
	{
		using MagickImage magickImage = (MagickImage)image.Clone();
		Prepare(magickImage, format, quality, keepMetadata);
		return magickImage.ToByteArray(format.Magick);
	}

	public static void Save(MagickImage image, ImageFormat format, string path, double quality, bool keepMetadata = true)
	{
		using MagickImage magickImage = (MagickImage)image.Clone();
		Prepare(magickImage, format, quality, keepMetadata);
		magickImage.Write(path, format.Magick);
	}

	private static void Prepare(MagickImage image, ImageFormat format, double quality, bool keepMetadata)
	{
		image.Format = format.Magick;
		image.Quality = (uint)Math.Clamp(Math.Round(quality * 100.0), 1.0, 100.0);
		if (!keepMetadata)
		{
			IColorProfile colorProfile = image.GetColorProfile();
			image.Strip();
			if (colorProfile != null)
			{
				image.SetProfile(colorProfile);
			}
		}
		if ((format == ImageFormat.Jpeg || format == ImageFormat.Bmp) && image.HasAlpha)
		{
			image.BackgroundColor = MagickColors.White;
			image.Alpha(AlphaAction.Remove);
		}
		if (format == ImageFormat.Jpeg)
		{
			image.Settings.SetDefine(MagickFormat.Jpeg, "sampling-factor", "4:2:0");
		}
		if (format == ImageFormat.WebP)
		{
			image.Settings.SetDefine(MagickFormat.WebP, "method", "5");
		}
		if (format == ImageFormat.Png)
		{
			image.Settings.SetDefine(MagickFormat.Png, "compression-level", "9");
		}
		if (format == ImageFormat.Ico)
		{
			if (Math.Max(image.Width, image.Height) > 256)
			{
				image.Resize(new MagickGeometry(256u, 256u));
			}
			image.Settings.SetDefine(MagickFormat.Ico, "auto-resize", "256,128,64,48,32,16");
		}
		if (format == ImageFormat.Gif)
		{
			image.Quantize(new QuantizeSettings
			{
				Colors = 256u,
				DitherMethod = DitherMethod.FloydSteinberg
			});
		}
	}

	public static long Size(string path)
	{
		return new FileInfo(path).Length;
	}
}
