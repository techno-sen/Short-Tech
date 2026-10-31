using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using ZXing;
using ZXing.Common;
using ZXing.Multi;

namespace ShortTech.Core.Images;

public static class Recognition
{
	public static async Task<string> Text(MagickImage image)
	{
		List<OcrEngine> engines = Engines();
		if (engines.Count == 0)
		{
			throw new ShortTechException("No text recognition languages are installed in Windows.");
		}
		using SoftwareBitmap bitmap = await ToSoftwareBitmap(image, (int)OcrEngine.MaxImageDimension);
		string best = "";
		foreach (OcrEngine item in engines)
		{
			OcrResult ocrResult = await item.RecognizeAsync(bitmap);
			string text = string.Join(Environment.NewLine, ocrResult.Lines.Select((OcrLine line) => line.Text));
			if (text.Count(char.IsLetterOrDigit) > best.Count(char.IsLetterOrDigit))
			{
				best = text;
			}
		}
		return best.Trim();
	}

	private static List<OcrEngine> Engines()
	{
		List<OcrEngine> list = new List<OcrEngine>();
		string[] array = new string[3] { "ar", "en-US", "en-GB" };
		for (int i = 0; i < array.Length; i++)
		{
			Language language = new Language(array[i]);
			if (OcrEngine.IsLanguageSupported(language))
			{
				OcrEngine ocrEngine = OcrEngine.TryCreateFromLanguage(language);
				if ((object)ocrEngine != null)
				{
					list.Add(ocrEngine);
				}
			}
		}
		if (list.Count == 0)
		{
			OcrEngine ocrEngine2 = OcrEngine.TryCreateFromUserProfileLanguages();
			if ((object)ocrEngine2 != null)
			{
				list.Add(ocrEngine2);
			}
		}
		return list;
	}

	public static IReadOnlyList<string> Codes(MagickImage image)
	{
		using MagickImage magickImage = (MagickImage)image.Clone();
		if (Math.Max(magickImage.Width, magickImage.Height) > 2400)
		{
			magickImage.Resize(new MagickGeometry(2400u, 2400u));
		}
		magickImage.ColorSpace = ColorSpace.Gray;
		using IPixelCollection<byte> pixelCollection = magickImage.GetPixels();
		byte[] array = pixelCollection.ToByteArray(PixelMapping.RGB);
		byte[] array2 = new byte[magickImage.Width * magickImage.Height];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = array[i * 3];
		}
		RGBLuminanceSource source = new RGBLuminanceSource(array2, (int)magickImage.Width, (int)magickImage.Height, RGBLuminanceSource.BitmapFormat.Gray8);
		return new GenericMultipleBarcodeReader(new MultiFormatReader()).decodeMultiple(hints: new Dictionary<DecodeHintType, object> { [DecodeHintType.TRY_HARDER] = true }, image: new BinaryBitmap(new HybridBinarizer(source)))?.Select((Result result) => result.Text).Distinct().ToList() ?? new List<string>();
	}

	public static async Task<IReadOnlyList<(double X, double Y, double Width, double Height)>> Faces(MagickImage image)
	{
		if (!FaceDetector.IsSupported)
		{
			return Array.Empty<(double, double, double, double)>();
		}
		FaceDetector detector = await FaceDetector.CreateAsync();
		using SoftwareBitmap bitmap = await ToSoftwareBitmap(image, 2400);
		BitmapPixelFormat format = FaceDetector.GetSupportedBitmapPixelFormats().First();
		using SoftwareBitmap converted = SoftwareBitmap.Convert(bitmap, format);
		double scale = (double)image.Width / (double)bitmap.PixelWidth;
		return (await detector.DetectFacesAsync(converted)).Select((DetectedFace face) => ((double)face.FaceBox.X * scale, (double)face.FaceBox.Y * scale, (double)face.FaceBox.Width * scale, (double)face.FaceBox.Height * scale)).ToList();
	}

	public static async Task<IReadOnlyList<(double X, double Y, double Width, double Height)>> TextBoxes(MagickImage image)
	{
		OcrEngine engine = Engines().FirstOrDefault() ?? throw new ShortTechException("No text recognition languages are installed in Windows.");
		using SoftwareBitmap bitmap = await ToSoftwareBitmap(image, (int)OcrEngine.MaxImageDimension);
		double scale = (double)image.Width / (double)bitmap.PixelWidth;
		return (await engine.RecognizeAsync(bitmap)).Lines.Select(delegate(OcrLine line)
		{
			double num = line.Words.Min((OcrWord w) => w.BoundingRect.X);
			double num2 = line.Words.Min((OcrWord w) => w.BoundingRect.Y);
			double num3 = line.Words.Max((OcrWord w) => w.BoundingRect.X + w.BoundingRect.Width);
			double num4 = line.Words.Max((OcrWord w) => w.BoundingRect.Y + w.BoundingRect.Height);
			return (num * scale, num2 * scale, (num3 - num) * scale, (num4 - num2) * scale);
		}).ToList();
	}

	private static async Task<SoftwareBitmap> ToSoftwareBitmap(MagickImage image, int maxDimension)
	{
		using MagickImage copy = (MagickImage)image.Clone();
		if (Math.Max(copy.Width, copy.Height) > maxDimension)
		{
			copy.Resize(new MagickGeometry((uint)maxDimension, (uint)maxDimension));
		}
		byte[] value = copy.ToByteArray(MagickFormat.Png);
		using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
		using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0uL)))
		{
			writer.WriteBytes(value);
			await writer.StoreAsync();
		}
		return await (await BitmapDecoder.CreateAsync(stream)).GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
	}
}
