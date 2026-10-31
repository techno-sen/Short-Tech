using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ShortTech.Core.Images;
using ShortTech.Core.Pdf;
using ImageMagick;

namespace ShortTech.Core.Documents;

public static class SlideOps
{
	public static IReadOnlyList<string> Targets => new global::_003C_003Ez__ReadOnlyArray<string>(new string[4] { "pdf", "pptx", "png", "jpg" });

	public static string Title(string target)
	{
		if (target == "jpg")
		{
			return "JPEG";
		}
		return target.ToUpperInvariant();
	}

	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, string target, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Converting {index + 1} of {files.Count}");
			string file = files[index];
			if (!(target == "pdf"))
			{
				if (target == "pptx")
				{
					string output = Output.PathFor(file, "pptx");
					await Office.SlidesToPptx(file, output);
					outputs.Add(output);
					continue;
				}
				using TempFolder temp = Output.Temp();
				string pdf = temp.File("slides.pdf");
				await Office.SlidesToPdf(file, pdf);
				ImageFormat format = ((target == "png") ? ImageFormat.Png : ImageFormat.Jpeg);
				string folder = Output.Named(Path.GetFileNameWithoutExtension(file) + " slides", file);
				Directory.CreateDirectory(folder);
				int count = PdfOps.PageCount(pdf);
				int page;
				for (page = 0; page < count; page++)
				{
					context.Report($"Rendering page {page + 1} of {count}");
					await Task.Run(delegate
					{
						using MagickImage image = PdfOps.RenderPage(pdf, page, 200);
						ImageEngine.Save(image, format, Path.Combine(folder, $"Slide {page + 1}.{format.Ext}"), 0.9);
					});
				}
				outputs.Add(folder);
			}
			else
			{
				string output = Output.PathFor(file, "pdf");
				await Office.SlidesToPdf(file, output);
				outputs.Add(output);
			}
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> PdfToPptx(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			string output = Output.PathFor(file, "pptx");
			await Task.Run(delegate
			{
				int num = PdfOps.PageCount(file);
				List<(byte[], long, long)> list = new List<(byte[], long, long)>();
				for (int i = 0; i < num; i++)
				{
					context.Report($"Building slide {i + 1} of {num}");
					context.Progress((double)i / (double)num);
					using MagickImage magickImage = PdfOps.RenderPage(file, i, 200);
					list.Add((magickImage.ToByteArray(MagickFormat.Png), (long)magickImage.Width * 914400L / 200, (long)magickImage.Height * 914400L / 200));
				}
				Pptx.Write(list, output);
			});
			outputs.Add(output);
		}
		return outputs;
	}
}
