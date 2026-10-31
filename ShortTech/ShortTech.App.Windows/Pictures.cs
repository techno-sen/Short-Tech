using System;
using System.IO;
using System.Windows.Media.Imaging;
using ShortTech.Core.Images;
using ImageMagick;

namespace ShortTech.App.Windows;

public static class Pictures
{
	public static BitmapSource ToBitmap(MagickImage image)
	{
		byte[] buffer = image.ToByteArray(MagickFormat.Png);
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = new MemoryStream(buffer);
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		return bitmapImage;
	}

	public static BitmapSource? FromBytes(byte[]? bytes)
	{
		if (bytes == null || bytes.Length == 0)
		{
			return null;
		}
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = new MemoryStream(bytes);
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		return bitmapImage;
	}

	public static (MagickImage Preview, int Width, int Height) LoadPreview(string path, int maxPixels = 1400)
	{
		MagickImage magickImage = ImageEngine.Load(path);
		int width = (int)magickImage.Width;
		int height = (int)magickImage.Height;
		if (Math.Max(width, height) > maxPixels)
		{
			magickImage.Resize(new MagickGeometry((uint)maxPixels, (uint)maxPixels));
		}
		return (Preview: magickImage, Width: width, Height: height);
	}
}
