using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImageMagick;

namespace ShortTech.Core.Images;

public sealed record ImageFormat(string Name, string Ext, MagickFormat Magick, bool Lossy)
{
	public bool CanWrite => MagickFormatInfo.Create(Magick)?.SupportsWriting ?? false;

	public static readonly ImageFormat Png = new ImageFormat("PNG", "png", MagickFormat.Png, Lossy: false);

	public static readonly ImageFormat Jpeg = new ImageFormat("JPEG", "jpg", MagickFormat.Jpeg, Lossy: true);

	public static readonly ImageFormat Heic = new ImageFormat("HEIC", "heic", MagickFormat.Heic, Lossy: true);

	public static readonly ImageFormat WebP = new ImageFormat("WebP", "webp", MagickFormat.WebP, Lossy: true);

	public static readonly ImageFormat Avif = new ImageFormat("AVIF", "avif", MagickFormat.Avif, Lossy: true);

	public static readonly ImageFormat Tiff = new ImageFormat("TIFF", "tiff", MagickFormat.Tiff, Lossy: false);

	public static readonly ImageFormat Gif = new ImageFormat("GIF", "gif", MagickFormat.Gif, Lossy: false);

	public static readonly ImageFormat Bmp = new ImageFormat("BMP", "bmp", MagickFormat.Bmp, Lossy: false);

	public static readonly ImageFormat Ico = new ImageFormat("ICO", "ico", MagickFormat.Ico, Lossy: false);

	public static readonly IReadOnlyList<ImageFormat> All = new global::_003C_003Ez__ReadOnlyArray<ImageFormat>(new ImageFormat[9] { Png, Jpeg, Heic, WebP, Avif, Tiff, Gif, Bmp, Ico });

	public static ImageFormat? Of(string path)
	{
		string text = FileKinds.Ext(path);
		switch (text)
		{
		case "jpeg":
		case "jfif":
			return Jpeg;
		case "tif":
			return Tiff;
		case "heif":
			return Heic;
		default:
			return All.FirstOrDefault((ImageFormat format) => format.Ext == text);
		}
	}

	[CompilerGenerated]
	private ImageFormat(ImageFormat original)
	{
		Name = original.Name;
		Ext = original.Ext;
		Magick = original.Magick;
		Lossy = original.Lossy;
	}
}
