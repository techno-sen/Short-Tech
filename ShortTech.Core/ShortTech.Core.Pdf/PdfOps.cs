using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ShortTech.Core.Documents;
using ShortTech.Core.Images;
using ClosedXML.Excel;
using ImageMagick;
using PDFtoImage;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace ShortTech.Core.Pdf;

public static class PdfOps
{
	internal sealed record TextLine(string Text, double Top, double Bottom, double Left, double Right, double FontSize, bool Bold, List<Word> Words);

	public static int PageCount(string path)
	{
		using FileStream pdfStream = File.OpenRead(path);
		return Conversion.GetPageCount(pdfStream);
	}

	public static MagickImage RenderPage(string path, int page, int dpi = 150)
	{
		using FileStream pdfStream = File.OpenRead(path);
		Index page2 = page;
		SKColor? backgroundColor = SKColors.White;
		using SKBitmap sKBitmap = Conversion.ToImage(pdfStream, page2, leaveOpen: false, null, new RenderOptions(dpi, null, null, WithAnnotations: true, WithFormFill: true, WithAspectRatio: false, PdfRotation.Rotate0, PdfAntiAliasing.All, backgroundColor));
		using SKData sKData = sKBitmap.Encode(SKEncodedImageFormat.Png, 100);
		return new MagickImage(sKData.ToArray());
	}

	public static async Task<IReadOnlyList<string>> RenderPages(IReadOnlyList<string> files, ImageFormat format, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			int count = PageCount(file);
			string folder = ((count > 1) ? Output.Named(Path.GetFileNameWithoutExtension(file) + " pages", file) : null);
			if (folder != null)
			{
				Directory.CreateDirectory(folder);
			}
			for (int page = 0; page < count; page++)
			{
				context.Report($"Rendering page {page + 1} of {count}");
				context.Progress((double)page / (double)count);
				string output = ((folder != null) ? Path.Combine(folder, $"Page {page + 1}.{format.Ext}") : Output.PathFor(file, format.Ext));
				await Task.Run(delegate
				{
					using MagickImage image = RenderPage(file, page, 300);
					ImageEngine.Save(image, format, output, 0.9);
				});
				if (folder == null)
				{
					outputs.Add(output);
				}
			}
			if (folder != null)
			{
				outputs.Add(folder);
			}
		}
		return outputs;
	}

	public static async Task<string> Text(string path, OperationContext context)
	{
		List<string> pages = new List<string>();
		int count = PageCount(path);
		using UglyToad.PdfPig.PdfDocument pdf = UglyToad.PdfPig.PdfDocument.Open(path);
		int index;
		for (index = 0; index < count; index++)
		{
			context.Progress((double)index / (double)count);
			Page page = pdf.GetPage(index + 1);
			string text = string.Join("\n", from line in Lines(page)
				select line.Text);
			if (text.Count(char.IsLetterOrDigit) < 20)
			{
				context.Report($"Recognizing page {index + 1} of {count}");
				using MagickImage image = await Task.Run(() => RenderPage(path, index, 200));
				text = await Recognition.Text(image);
			}
			pages.Add(text.Trim());
		}
		return string.Join("\n\n", pages.Where((string p) => p.Length > 0));
	}

	public static async Task<IReadOnlyList<string>> ToText(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string output = Output.PathFor(file, "txt");
			string path = output;
			await File.WriteAllTextAsync(path, await Text(file, context), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			outputs.Add(output);
		}
		return outputs;
	}

	internal static List<TextLine> Lines(Page page)
	{
		List<Word> source = (from w in page.GetWords(NearestNeighbourWordExtractor.Instance)
			where w.Text.Trim().Length > 0
			select w).ToList();
		List<List<Word>> list = new List<List<Word>>();
		foreach (Word word in source.OrderByDescending((Word w) => w.BoundingBox.Bottom))
		{
			double height = Math.Max(1.0, word.BoundingBox.Height);
			List<Word> list2 = list.FirstOrDefault((List<Word> l) => Math.Abs(l[0].BoundingBox.Bottom - word.BoundingBox.Bottom) < height * 0.5);
			if (list2 != null)
			{
				list2.Add(word);
				continue;
			}
			int num = 1;
			List<Word> list3 = new List<Word>(num);
			CollectionsMarshal.SetCount(list3, num);
			CollectionsMarshal.AsSpan(list3)[0] = word;
			list.Add(list3);
		}
		return (from l in list.Select(delegate(List<Word> line)
			{
				List<Word> list4 = ((line.Sum((Word w) => w.Text.Count(IsArabic)) > line.Sum((Word w) => w.Text.Count(char.IsLetter)) / 2) ? line.OrderByDescending((Word w) => w.BoundingBox.Left).ToList() : line.OrderBy((Word w) => w.BoundingBox.Left).ToList());
				string text = string.Join(" ", list4.Select((Word w) => Logical(w)));
				List<Letter> list5 = line.SelectMany((Word w) => w.Letters).ToList();
				return new TextLine(FontSize: (list5.Count > 0) ? list5.Average((Letter l) => l.PointSize) : 10.0, Bold: list5.Count > 0 && list5.Count((Letter l) => l.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) ?? false) > list5.Count / 2, Text: text, Top: line.Max((Word w) => w.BoundingBox.Top), Bottom: line.Min((Word w) => w.BoundingBox.Bottom), Left: line.Min((Word w) => w.BoundingBox.Left), Right: line.Max((Word w) => w.BoundingBox.Right), Words: list4);
			})
			orderby l.Top descending
			select l).ToList();
	}

	private static bool IsArabic(char c)
	{
		if (c >= 'ﭐ')
		{
			if (c >= 'ﹰ')
			{
				if (c <= '\ufeff')
				{
					goto IL_0046;
				}
			}
			else if (c <= '﷿')
			{
				goto IL_0046;
			}
		}
		else if (c >= 'ݐ')
		{
			if (c <= 'ݿ')
			{
				goto IL_0046;
			}
		}
		else if (c >= '\u0600' && c <= 'ۿ')
		{
			goto IL_0046;
		}
		return false;
		IL_0046:
		return true;
	}

	private static string Logical(Word word)
	{
		string text = word.Text;
		if (!text.Any(IsArabic))
		{
			return text;
		}
		List<Letter> list = word.Letters.Where((Letter l) => l.Value.Length > 0).ToList();
		if (list.Count > 1)
		{
			if (list[0].StartBaseLine.X < list[list.Count - 1].StartBaseLine.X)
			{
				text = string.Concat(from l in list
					orderby l.StartBaseLine.X descending
					select l.Value);
			}
		}
		return text.Normalize(NormalizationForm.FormKC);
	}

	public static async Task<Doc> ReadDocument(string path)
	{
		Doc doc = new Doc
		{
			Title = Path.GetFileNameWithoutExtension(path)
		};
		double body;
		List<TextLine> paragraph;
		using (UglyToad.PdfPig.PdfDocument pdf = UglyToad.PdfPig.PdfDocument.Open(path))
		{
			List<double> list = (from s in pdf.GetPages().SelectMany((Page p) => p.Letters.Select((Letter l) => l.PointSize))
				where s > 0.0
				orderby s
				select s).ToList();
			body = ((list.Count > 0) ? list[list.Count / 2] : 10.0);
			int index;
			for (index = 1; index <= pdf.NumberOfPages; index++)
			{
				if (index > 1)
				{
					doc.Blocks.Add(new PageBreak());
				}
				Page page = pdf.GetPage(index);
				List<TextLine> list2 = Lines(page);
				if (list2.Sum((TextLine l) => l.Text.Count(char.IsLetterOrDigit)) < 20)
				{
					using (MagickImage image = await Task.Run(() => RenderPage(path, index - 1, 200)))
					{
						string[] array = (await Recognition.Text(image)).Split("\n\n");
						foreach (string text in array)
						{
							if (text.Trim().Length > 0)
							{
								doc.Blocks.Add(new Paragraph(new List<Span>(1)
								{
									new Span(text.Trim())
								}));
							}
						}
					}
					continue;
				}
				paragraph = new List<TextLine>();
				for (int num2 = 0; num2 < list2.Count; num2++)
				{
					TextLine textLine = list2[num2];
					if (paragraph.Count > 0)
					{
						List<TextLine> list3 = paragraph;
						TextLine textLine2 = list3[list3.Count - 1];
						double num3 = textLine2.Bottom - textLine.Top;
						double num4 = Math.Max(1.0, textLine2.Top - textLine2.Bottom);
						if (num3 > num4 * 0.9 || Math.Abs(textLine.FontSize - textLine2.FontSize) > 1.5 || textLine2.Bold != textLine.Bold || textLine.Text.TrimStart().StartsWith("•"))
						{
							Flush();
						}
					}
					paragraph.Add(textLine);
				}
				Flush();
				foreach (IPdfImage image2 in page.GetImages())
				{
					if (image2.BoundingBox.Width < 40.0 || image2.BoundingBox.Height < 40.0)
					{
						continue;
					}
					if (image2.TryGetPng(out byte[] bytes))
					{
						doc.Blocks.Add(new Picture(bytes, "png"));
						continue;
					}
					byte[] array2 = image2.RawBytes.ToArray();
					if (array2.Length > 2 && array2[0] == byte.MaxValue && array2[1] == 216)
					{
						doc.Blocks.Add(new Picture(array2, "jpg"));
					}
				}
			}
			return doc;
		}
		void Flush()
		{
			if (paragraph.Count != 0)
			{
				string text2 = string.Join(" ", paragraph.Select((TextLine l) => l.Text));
				double num5 = paragraph.Average((TextLine l) => l.FontSize);
				bool flag = paragraph.All((TextLine l) => l.Bold);
				if (num5 > body * 1.6 && text2.Length < 160)
				{
					List<Block> blocks = doc.Blocks;
					int num6 = 1;
					List<Span> list4 = new List<Span>(num6);
					CollectionsMarshal.SetCount(list4, num6);
					CollectionsMarshal.AsSpan(list4)[0] = new Span(text2);
					blocks.Add(new Heading(1, list4));
				}
				else if (num5 > body * 1.25 && text2.Length < 160)
				{
					List<Block> blocks2 = doc.Blocks;
					int num6 = 1;
					List<Span> list5 = new List<Span>(num6);
					CollectionsMarshal.SetCount(list5, num6);
					CollectionsMarshal.AsSpan(list5)[0] = new Span(text2);
					blocks2.Add(new Heading(2, list5));
				}
				else if (flag && text2.Length < 100 && paragraph.Count == 1)
				{
					List<Block> blocks3 = doc.Blocks;
					int num6 = 1;
					List<Span> list6 = new List<Span>(num6);
					CollectionsMarshal.SetCount(list6, num6);
					CollectionsMarshal.AsSpan(list6)[0] = new Span(text2);
					blocks3.Add(new Heading(3, list6));
				}
				else if (text2.TrimStart().StartsWith("•") || text2.TrimStart().StartsWith("- "))
				{
					List<Block> blocks4 = doc.Blocks;
					int num6 = 1;
					List<Span> list7 = new List<Span>(num6);
					CollectionsMarshal.SetCount(list7, num6);
					CollectionsMarshal.AsSpan(list7)[0] = new Span(text2.TrimStart('•', '-', ' '));
					blocks4.Add(new ListItem(list7, Ordered: false));
				}
				else
				{
					List<Block> blocks5 = doc.Blocks;
					int num6 = 1;
					List<Span> list8 = new List<Span>(num6);
					CollectionsMarshal.SetCount(list8, num6);
					CollectionsMarshal.AsSpan(list8)[0] = new Span(text2);
					blocks5.Add(new Paragraph(list8));
				}
				paragraph.Clear();
			}
		}
	}

	public static async Task<IReadOnlyList<string>> ToDocument(IReadOnlyList<string> files, DocumentTarget target, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Converting {index + 1} of {files.Count}");
			string text = files[index];
			string output = Output.PathFor(text, target.Ext());
			await DocumentOps.Write(await ReadDocument(text), target, output);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> ToExcel(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string output = Output.PathFor(file, "xlsx");
			await Task.Run(delegate
			{
				using UglyToad.PdfPig.PdfDocument pdfDocument = UglyToad.PdfPig.PdfDocument.Open(file);
				using XLWorkbook xLWorkbook = new XLWorkbook();
				for (int i = 1; i <= pdfDocument.NumberOfPages; i++)
				{
					context.Report($"Page {i} of {pdfDocument.NumberOfPages}");
					IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add($"Page {i}");
					List<TextLine> list = Lines(pdfDocument.GetPage(i));
					bool flag = list.Sum((TextLine l) => l.Text.Count(IsArabic)) > list.Sum((TextLine l) => l.Text.Length) / 3;
					if (flag)
					{
						iXLWorksheet.RightToLeft = true;
					}
					int num = 1;
					foreach (TextLine item in list)
					{
						List<string> list2 = Cells(item, flag);
						for (int num2 = 0; num2 < list2.Count; num2++)
						{
							string text = list2[num2];
							IXLCell iXLCell = iXLWorksheet.Cell(num, num2 + 1);
							if (double.TryParse(text.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && !text.StartsWith('0'))
							{
								iXLCell.Value = result;
							}
							else
							{
								iXLCell.Value = text;
							}
						}
						num++;
					}
					iXLWorksheet.Columns().AdjustToContents(1, 200);
				}
				if (xLWorkbook.Worksheets.Count == 0)
				{
					xLWorkbook.Worksheets.Add("Page 1");
				}
				xLWorkbook.SaveAs(output);
			});
			outputs.Add(output);
		}
		return outputs;
	}

	private static List<string> Cells(TextLine line, bool rtl)
	{
		List<Word> words = line.Words;
		if (words.Count == 0)
		{
			return new List<string>();
		}
		double num = words.Average((Word w) => w.BoundingBox.Width / (double)Math.Max(1, w.Text.Length));
		List<List<Word>> list = new List<List<Word>>
		{
			new List<Word> { words[0] }
		};
		for (int num2 = 1; num2 < words.Count; num2++)
		{
			if ((rtl ? (words[num2 - 1].BoundingBox.Left - words[num2].BoundingBox.Right) : (words[num2].BoundingBox.Left - words[num2 - 1].BoundingBox.Right)) > num * 2.2)
			{
				int num3 = 1;
				List<Word> list2 = new List<Word>(num3);
				CollectionsMarshal.SetCount(list2, num3);
				CollectionsMarshal.AsSpan(list2)[0] = words[num2];
				list.Add(list2);
			}
			else
			{
				list[list.Count - 1].Add(words[num2]);
			}
		}
		return list.Select((List<Word> cell) => string.Join(" ", cell.Select(Logical))).ToList();
	}

	private static PdfSharp.Pdf.PdfDocument Open(string path, PdfDocumentOpenMode mode)
	{
		try
		{
			return PdfReader.Open(path, mode);
		}
		catch (PdfReaderException)
		{
			throw new ShortTechException(Path.GetFileName(path) + " cannot be read or is password-protected");
		}
		catch (InvalidOperationException)
		{
			throw new ShortTechException(Path.GetFileName(path) + " cannot be read or is password-protected");
		}
	}

	public static async Task<IReadOnlyList<string>> Merge(IReadOnlyList<string> files, OperationContext context)
	{
		string output = Output.Named(Path.GetFileNameWithoutExtension(files[0]) + " merged.pdf", files[0]);
		int pages = 0;
		await Task.Run(delegate
		{
			using PdfSharp.Pdf.PdfDocument pdfDocument = new PdfSharp.Pdf.PdfDocument();
			pdfDocument.Info.Creator = "Short Tech";
			for (int i = 0; i < files.Count; i++)
			{
				context.Progress((double)i / (double)files.Count);
				using PdfSharp.Pdf.PdfDocument pdfDocument2 = Open(files[i], PdfDocumentOpenMode.Import);
				foreach (PdfPage page in pdfDocument2.Pages)
				{
					pdfDocument.AddPage(page);
					pages++;
				}
			}
			pdfDocument.Save(output);
		});
		context.Finish($"Merged {files.Count} PDFs · {pages} pages");
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
	}

	public static async Task<IReadOnlyList<string>> Split(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string folder = Output.Named(Path.GetFileNameWithoutExtension(file) + " pages", file);
			await Task.Run(delegate
			{
				using PdfSharp.Pdf.PdfDocument pdfDocument = Open(file, PdfDocumentOpenMode.Import);
				if (pdfDocument.PageCount < 2)
				{
					throw new ShortTechException(Path.GetFileName(file) + " has no pages");
				}
				Directory.CreateDirectory(folder);
				for (int i = 0; i < pdfDocument.PageCount; i++)
				{
					context.Progress((double)i / (double)pdfDocument.PageCount);
					using PdfSharp.Pdf.PdfDocument pdfDocument2 = new PdfSharp.Pdf.PdfDocument();
					pdfDocument2.AddPage(pdfDocument.Pages[i]);
					pdfDocument2.Save(Path.Combine(folder, $"Page {i + 1}.pdf"));
				}
			});
			outputs.Add(folder);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> StripMetadata(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string output = Output.PathFor(file, "pdf", "clean");
			await Task.Run(delegate
			{
				using PdfSharp.Pdf.PdfDocument pdfDocument = Open(file, PdfDocumentOpenMode.Modify);
				pdfDocument.Info.Elements.Clear();
				pdfDocument.Internals.Catalog.Elements.Remove("/Metadata");
				foreach (PdfPage page in pdfDocument.Pages)
				{
					page.Elements.Remove("/Metadata");
				}
				pdfDocument.Options.CompressContentStreams = true;
				pdfDocument.Save(output);
			});
			outputs.Add(output);
		}
		context.Finish((files.Count == 1) ? "Metadata removed from 1 PDF" : $"Metadata removed from {files.Count} PDFs");
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Protect(IReadOnlyList<string> files, string password, OperationContext context)
	{
		if (string.IsNullOrEmpty(password))
		{
			throw new ShortTechException("Enter a password.");
		}
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			context.Progress((double)index / (double)files.Count);
			string output = Output.PathFor(file, "pdf", "protected");
			await Task.Run(delegate
			{
				using PdfSharp.Pdf.PdfDocument pdfDocument = Open(file, PdfDocumentOpenMode.Modify);
				pdfDocument.SecurityHandler.SetEncryptionToV5(true);
				pdfDocument.SecuritySettings.UserPassword = password;
				pdfDocument.SecuritySettings.OwnerPassword = password;
				pdfDocument.Save(output);
			});
			outputs.Add(output);
		}
		context.Finish((files.Count == 1) ? "Protected 1 PDF" : $"Protected {files.Count} PDFs");
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Compress(IReadOnlyList<string> files, OperationContext context)
	{
		(int, int) tuple = Settings.Current.CompressPreset switch
		{
			CompressPreset.Light => (2400, 82), 
			CompressPreset.Balanced => (1800, 68), 
			_ => (1300, 52), 
		};
		int maxPixels = tuple.Item1;
		int quality = tuple.Item2;
		List<string> outputs = new List<string>();
		long before = 0L;
		long after = 0L;
		foreach (string file in files)
		{
			string output = Output.PathFor(file, "pdf", "compressed");
			await Task.Run(delegate
			{
				using PdfSharp.Pdf.PdfDocument pdfDocument = Open(file, PdfDocumentOpenMode.Modify);
				HashSet<PdfDictionary> seen = new HashSet<PdfDictionary>();
				for (int i = 0; i < pdfDocument.PageCount; i++)
				{
					context.Progress((double)i / (double)pdfDocument.PageCount);
					foreach (PdfDictionary item in Images(pdfDocument.Pages[i].Elements.GetDictionary("/Resources"), seen))
					{
						Recompress(item, maxPixels, quality);
					}
				}
				pdfDocument.Options.CompressContentStreams = true;
				pdfDocument.Options.NoCompression = false;
				pdfDocument.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
				pdfDocument.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Never;
				pdfDocument.Save(output);
			});
			long length = new FileInfo(file).Length;
			if (new FileInfo(output).Length >= length)
			{
				File.Delete(output);
				continue;
			}
			before += length;
			after += new FileInfo(output).Length;
			outputs.Add(output);
		}
		if (outputs.Count == 0)
		{
			throw new ShortTechException("These PDFs are already as small as they get");
		}
		context.Finish((outputs.Count == 1) ? $"Compressed PDF · {100 - (int)Math.Round((double)after / (double)before * 100.0)}% smaller" : Compression.Savings("PDF", outputs.Count, before, after));
		return outputs;
	}

	private static IEnumerable<PdfDictionary> Images(PdfDictionary? resources, HashSet<PdfDictionary> seen)
	{
		PdfDictionary objects = resources?.Elements.GetDictionary("/XObject");
		if (objects == null)
		{
			yield break;
		}
		foreach (string key in objects.Elements.Keys)
		{
			if (!(objects.Elements.GetReference(key)?.Value is PdfDictionary pdfDictionary) || !seen.Add(pdfDictionary))
			{
				continue;
			}
			string name = pdfDictionary.Elements.GetName("/Subtype");
			if (name == "/Image")
			{
				yield return pdfDictionary;
			}
			else
			{
				if (!(name == "/Form"))
				{
					continue;
				}
				foreach (PdfDictionary item in Images(pdfDictionary.Elements.GetDictionary("/Resources"), seen))
				{
					yield return item;
				}
			}
		}
	}

	private static void Recompress(PdfDictionary image, int maxPixels, int quality)
	{
		if (image.Stream == null || image.Elements.ContainsKey("/ImageMask") || image.Elements.GetBoolean("/ImageMask"))
		{
			return;
		}
		string text = image.Elements["/Filter"]?.ToString() ?? "";
		int integer = image.Elements.GetInteger("/Width");
		int integer2 = image.Elements.GetInteger("/Height");
		int integer3 = image.Elements.GetInteger("/BitsPerComponent");
		string text2 = image.Elements["/ColorSpace"]?.ToString() ?? "";
		if (integer < 64 || integer2 < 64)
		{
			return;
		}
		MagickImage magickImage = null;
		try
		{
			if (text.Contains("/DCTDecode") && !text.Contains('['))
			{
				magickImage = new MagickImage(image.Stream.Value);
			}
			else
			{
				bool flag = text.Contains("/FlateDecode") && !text.Contains('[') && integer3 == 8;
				if (flag)
				{
					bool flag2 = ((text2 == "/DeviceRGB" || text2 == "/DeviceGray") ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					byte[] unfilteredValue = image.Stream.UnfilteredValue;
					int num = ((!(text2 == "/DeviceRGB")) ? 1 : 3);
					if (unfilteredValue.Length < integer * integer2 * num)
					{
						return;
					}
					magickImage = new MagickImage(unfilteredValue, new MagickReadSettings
					{
						Width = (uint)integer,
						Height = (uint)integer2,
						Format = ((num == 3) ? MagickFormat.Rgb : MagickFormat.Gray),
						Depth = 8u
					});
				}
			}
			if (magickImage != null && magickImage.ColorSpace != ColorSpace.CMYK)
			{
				if (Math.Max(magickImage.Width, magickImage.Height) > maxPixels)
				{
					magickImage.Resize(new MagickGeometry((uint)maxPixels, (uint)maxPixels));
				}
				magickImage.Quality = (uint)quality;
				magickImage.Strip();
				byte[] array = magickImage.ToByteArray(MagickFormat.Jpeg);
				if (!((double)array.Length >= (double)image.Stream.Length * 0.9))
				{
					image.Stream.Value = array;
					image.Elements.SetName("/Filter", "/DCTDecode");
					image.Elements.Remove("/DecodeParms");
					image.Elements.SetInteger("/Width", (int)magickImage.Width);
					image.Elements.SetInteger("/Height", (int)magickImage.Height);
					image.Elements.SetInteger("/BitsPerComponent", 8);
					image.Elements.SetName("/ColorSpace", (magickImage.ChannelCount >= 3) ? "/DeviceRGB" : "/DeviceGray");
				}
			}
		}
		catch (MagickException)
		{
		}
		finally
		{
			magickImage?.Dispose();
		}
	}

	public static async Task<string> Organize(string file, IReadOnlyList<PageSlot> slots, bool equalWidths, OperationContext context)
	{
		if (slots.Count == 0)
		{
			throw new ShortTechException("Keep at least one page.");
		}
		string output = Output.PathFor(file, "pdf", "organized");
		await Task.Run(delegate
		{
			PdfSharp.Pdf.PdfDocument source = Open(file, PdfDocumentOpenMode.Import);
			try
			{
				using PdfSharp.Pdf.PdfDocument pdfDocument = new PdfSharp.Pdf.PdfDocument();
				pdfDocument.Info.Creator = "Short Tech";
				double num = slots.Min((PageSlot slot) => Upright(source.Pages[slot.Source], slot.Rotation));
				using XPdfForm xPdfForm = (equalWidths ? XPdfForm.FromFile(file) : null);
				for (int num2 = 0; num2 < slots.Count; num2++)
				{
					context.Progress((double)num2 / (double)slots.Count);
					PageSlot pageSlot = slots[num2];
					PdfPage pdfPage = source.Pages[pageSlot.Source];
					double num3 = Upright(pdfPage, pageSlot.Rotation);
					if (!equalWidths || num3 <= num + 0.5)
					{
						pdfDocument.AddPage(pdfPage).Rotate = (pdfPage.Rotate + pageSlot.Rotation) % 360;
					}
					else
					{
						int num4 = (pdfPage.Rotate + pageSlot.Rotation) / 90 % 4;
						double num5 = ((num4 % 2 == 0) ? pdfPage.Width.Point : pdfPage.Height.Point);
						double num6 = ((num4 % 2 == 0) ? pdfPage.Height.Point : pdfPage.Width.Point);
						double num7 = num / num5;
						PdfPage pdfPage2 = pdfDocument.AddPage();
						pdfPage2.Width = XUnit.FromPoint(num);
						pdfPage2.Height = XUnit.FromPoint(num6 * num7);
						xPdfForm.PageIndex = pageSlot.Source;
						using XGraphics xGraphics = XGraphics.FromPdfPage(pdfPage2);
						xGraphics.ScaleTransform(num7);
						xGraphics.TranslateTransform(num5 / 2.0, num6 / 2.0);
						xGraphics.RotateTransform(num4 * 90);
						xGraphics.TranslateTransform((0.0 - pdfPage.Width.Point) / 2.0, (0.0 - pdfPage.Height.Point) / 2.0);
						xGraphics.DrawImage(xPdfForm, 0.0, 0.0, pdfPage.Width.Point, pdfPage.Height.Point);
					}
				}
				pdfDocument.Save(output);
			}
			finally
			{
				if (source != null)
				{
					((IDisposable)source).Dispose();
				}
			}
		});
		return output;
		static double Upright(PdfPage page, int extra)
		{
			if ((page.Rotate + extra) / 90 % 2 == 0)
			{
				return page.Width.Point;
			}
			return page.Height.Point;
		}
	}
}
