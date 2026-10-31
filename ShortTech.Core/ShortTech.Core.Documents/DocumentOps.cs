using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ShortTech.Core.Pdf;

namespace ShortTech.Core.Documents;

public static class DocumentOps
{
	public static async Task<Doc> Load(string path)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		Doc result;
		switch (FileKinds.Ext(path))
		{
		case "docx":
			result = await Task.Run(() => Docx.Read(path));
			break;
		case "doc":
			result = await Office.WordToDoc(path);
			break;
		case "md":
		case "markdown":
			result = MarkdownFormat.Read(ReadText(path), fileNameWithoutExtension);
			break;
		case "html":
		case "htm":
			result = HtmlReader.Read(ReadText(path), fileNameWithoutExtension, (string src) => LocalImage(path, src));
			break;
		case "epub":
			result = await Task.Run(() => Epub.Read(path));
			break;
		case "odt":
			result = await Task.Run(() => Odt.Read(path));
			break;
		case "rtf":
			result = await Task.Run(() => Rtf.Read(path));
			break;
		case "pdf":
			result = await PdfOps.ReadDocument(path);
			break;
		case "pages":
			throw new ShortTechException("Pages, Keynote and Numbers files are not supported on Windows.");
		default:
			result = Doc.FromText(ReadText(path), fileNameWithoutExtension);
			break;
		}
		return result;
	}

	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, DocumentTarget target, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Converting {index + 1} of {files.Count}");
			context.Progress((double)index / (double)files.Count);
			string text = files[index];
			string output = Output.PathFor(text, target.Ext());
			await Write(await Load(text), target, output);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task Write(Doc doc, DocumentTarget target, string output)
	{
		switch (target)
		{
		case DocumentTarget.Pdf:
			await PdfPrinter.Print(DocText.Html(doc, forPrint: true), output);
			break;
		case DocumentTarget.Docx:
			await Task.Run(delegate
			{
				Docx.Write(doc, output);
			});
			break;
		case DocumentTarget.Epub:
			await Task.Run(delegate
			{
				Epub.Write(doc, output);
			});
			break;
		case DocumentTarget.Md:
			await File.WriteAllTextAsync(output, DocText.Markdown(doc), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			break;
		case DocumentTarget.Txt:
			await File.WriteAllTextAsync(output, doc.PlainText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			break;
		case DocumentTarget.Rtf:
			await Task.Run(delegate
			{
				Rtf.Write(doc, output);
			});
			break;
		case DocumentTarget.Html:
			await File.WriteAllTextAsync(output, DocText.Html(doc), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			break;
		case DocumentTarget.Odt:
			await Task.Run(delegate
			{
				Odt.Write(doc, output);
			});
			break;
		}
	}

	public static string ReadText(string path)
	{
		byte[] array = File.ReadAllBytes(path);
		if (array.Length >= 2 && ((array[0] == byte.MaxValue && array[1] == 254) || (array[0] == 254 && array[1] == byte.MaxValue)))
		{
			return Encoding.Unicode.GetString(array).TrimStart('\ufeff');
		}
		try
		{
			return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(array).TrimStart('\ufeff');
		}
		catch (DecoderFallbackException)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			return Encoding.GetEncoding(1256).GetString(array);
		}
	}

	private static (byte[] Data, string Ext)? LocalImage(string page, string src)
	{
		if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string path = Path.Combine(Path.GetDirectoryName(page) ?? "", src.Replace('/', Path.DirectorySeparatorChar));
		if (!File.Exists(path))
		{
			return null;
		}
		string text = FileKinds.Ext(path);
		byte[] item = File.ReadAllBytes(path);
		bool flag = ((text == "png" || text == "gif") ? true : false);
		return (item, flag ? text : "jpg");
	}
}
