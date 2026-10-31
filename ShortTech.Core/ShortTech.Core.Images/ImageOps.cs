using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ShortTech.Core.Pdf;
using ImageMagick;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ShortTech.Core.Images;

public static class ImageOps
{
	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, ImageFormat format, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Converting {index + 1} of {files.Count}");
			context.Progress((double)index / (double)files.Count);
			string file = files[index];
			string output = Output.PathFor(file, format.Ext);
			await Task.Run(delegate
			{
				using MagickImage image = ImageEngine.Load(file);
				ImageEngine.Save(image, format, output, 0.9);
			});
			outputs.Add(output);
		}
		return outputs;
	}

	public static ImageFormat CompressedFormat(string path, MagickImage image)
	{
		ImageFormat imageFormat = ImageFormat.Of(path);
		if ((object)imageFormat != null && imageFormat.Lossy && imageFormat.CanWrite)
		{
			return imageFormat;
		}
		if (!image.HasAlpha)
		{
			return ImageFormat.Jpeg;
		}
		return ImageFormat.WebP;
	}

	public static ImageFormat EditedFormat(string path)
	{
		ImageFormat imageFormat = ImageFormat.Of(path) ?? ImageFormat.Png;
		if (!(imageFormat == ImageFormat.Gif) && !(imageFormat == ImageFormat.Ico) && imageFormat.CanWrite)
		{
			return imageFormat;
		}
		return ImageFormat.Png;
	}

	public static async Task<IReadOnlyList<string>> Resize(IReadOnlyList<string> files, int maxPixels, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Resizing {index + 1} of {files.Count}");
			string file = files[index];
			outputs.Add(await Task.Run(delegate
			{
				using MagickImage magickImage = ImageEngine.Load(file, maxPixels);
				string text = Output.PathFor(file, EditedFormat(file).Ext, $"{magickImage.Width}x{magickImage.Height}");
				ImageEngine.Save(magickImage, EditedFormat(file), text, 0.9);
				return text;
			}));
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> RemoveMetadata(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string output = Output.PathFor(file, Path.GetExtension(file).TrimStart('.'), "clean");
			await Task.Run(delegate
			{
				using MagickImage magickImage = new MagickImage(file);
				ImageFormat imageFormat = ImageFormat.Of(file) ?? ImageFormat.Png;
				magickImage.AutoOrient();
				IColorProfile colorProfile = magickImage.GetColorProfile();
				magickImage.Strip();
				if (colorProfile != null)
				{
					magickImage.SetProfile(colorProfile);
				}
				magickImage.Quality = 95u;
				magickImage.Write(output, imageFormat.Magick);
			});
			outputs.Add(output);
		}
		context.Finish((files.Count == 1) ? "Metadata removed from 1 image" : $"Metadata removed from {files.Count} images");
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Crop(IReadOnlyList<string> files, (double X, double Y, double W, double H) rect, OperationContext context, (int Width, int Height)? exactSize = null)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Cropping {index + 1} of {files.Count}");
			string file = files[index];
			string output = Output.PathFor(file, EditedFormat(file).Ext, "cropped");
			await Task.Run(delegate
			{
				using MagickImage magickImage = ImageEngine.Load(file);
				MagickGeometry geometry = new MagickGeometry((int)Math.Round(rect.X * (double)magickImage.Width), (int)Math.Round(rect.Y * (double)magickImage.Height), (uint)Math.Max(1.0, Math.Round(rect.W * (double)magickImage.Width)), (uint)Math.Max(1.0, Math.Round(rect.H * (double)magickImage.Height)));
				magickImage.Crop(geometry);
				magickImage.ResetPage();
				if (exactSize.HasValue)
				{
					(int, int) valueOrDefault = exactSize.GetValueOrDefault();
					magickImage.Resize(new MagickGeometry((uint)valueOrDefault.Item1, (uint)valueOrDefault.Item2)
					{
						IgnoreAspectRatio = true
					});
				}
				ImageEngine.Save(magickImage, EditedFormat(file), output, 0.92);
			});
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> SquareCrop(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			double num2;
			double num3;
			using (MagickImage magickImage = await ImageEngine.LoadAsync(file))
			{
				double num = magickImage.Width;
				num2 = magickImage.Height;
				num3 = num;
			}
			double num4 = Math.Min(num3, num2);
			List<string> list = outputs;
			list.AddRange(await Crop(new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), (X: (num3 - num4) / 2.0 / num3, Y: (num2 - num4) / 2.0 / num2, W: num4 / num3, H: num4 / num2), context));
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Rotate(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string output = Output.PathFor(file, Path.GetExtension(file).TrimStart('.'), "rotated");
			await Task.Run(delegate
			{
				if (ImageFormat.Of(file) == ImageFormat.Jpeg)
				{
					byte[] array = ExifOrientation.TryRotate(File.ReadAllBytes(file));
					if (array != null)
					{
						File.WriteAllBytes(output, array);
						return;
					}
				}
				using MagickImage magickImage = ImageEngine.Load(file);
				magickImage.Rotate(90.0);
				ImageEngine.Save(magickImage, ImageFormat.Of(file) ?? ImageFormat.Png, output, 0.95);
			});
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> MakePdf(IReadOnlyList<string> files, OperationContext context)
	{
		context.Report((files.Count == 1) ? "Creating PDF from 1 image" : $"Creating PDF from {files.Count} images");
		string text = ((files.Count == 1) ? Path.GetFileNameWithoutExtension(files[0]) : "Images");
		string output = Output.Named(text + ".pdf", files[0]);
		await Task.Run(delegate
		{
			using PdfDocument pdfDocument = new PdfDocument();
			pdfDocument.Info.Creator = "Short Tech";
			for (int i = 0; i < files.Count; i++)
			{
				context.Progress((double)i / (double)files.Count);
				using MagickImage magickImage = ImageEngine.Load(files[i]);
				bool flag = !magickImage.HasAlpha;
				using MemoryStream stream = new MemoryStream(ImageEngine.Encode(magickImage, flag ? ImageFormat.Jpeg : ImageFormat.Png, 0.92));
				using XImage image = XImage.FromStream(stream);
				double num = ((magickImage.Density.X > 1.0) ? magickImage.Density.ChangeUnits(DensityUnit.PixelsPerInch).X : 144.0);
				PdfPage pdfPage = pdfDocument.AddPage();
				pdfPage.Width = XUnit.FromPoint((double)(magickImage.Width * 72) / num);
				pdfPage.Height = XUnit.FromPoint((double)(magickImage.Height * 72) / num);
				using XGraphics xGraphics = XGraphics.FromPdfPage(pdfPage);
				xGraphics.DrawImage(image, 0.0, 0.0, pdfPage.Width.Point, pdfPage.Height.Point);
			}
			pdfDocument.Save(output);
		});
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
	}

	public static async Task<IReadOnlyList<string>> ExtractText(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		List<string> all = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Reading text {index + 1} of {files.Count}");
			string file = files[index];
			string text;
			if (FileKinds.Of(file) == FileKind.Pdf)
			{
				text = await PdfOps.Text(file, context);
			}
			else
			{
				using MagickImage image = await ImageEngine.LoadAsync(file);
				text = await Recognition.Text(image);
			}
			text = text.Trim();
			if (text.Length != 0)
			{
				string output = Output.PathFor(file, "txt");
				await File.WriteAllTextAsync(output, text);
				outputs.Add(output);
				all.Add(text);
				text = null;
			}
		}
		if (all.Count == 0)
		{
			throw new ShortTechException("No text found");
		}
		Clipboard.CopyText(string.Join(Environment.NewLine + Environment.NewLine, all));
		context.Finish("Text copied to clipboard");
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> ReadQrCodes(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> payloads = new List<string>();
		foreach (string file in files)
		{
			using MagickImage image = await ImageEngine.LoadAsync(file);
			payloads.AddRange(Recognition.Codes(image));
		}
		if (payloads.Count == 0)
		{
			throw new ShortTechException("No QR codes found");
		}
		Clipboard.CopyText(string.Join(Environment.NewLine, payloads));
		string text = ((payloads[0].Length > 60) ? (payloads[0].Substring(0, 60) + "…") : payloads[0]);
		context.Finish("Copied: " + text);
		return Array.Empty<string>();
	}
}
