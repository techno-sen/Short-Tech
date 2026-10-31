using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ShortTech.Core.Documents;

public static class Epub
{
	public static Doc Read(string path)
	{
		ZipArchive zip = ZipFile.OpenRead(path);
		try
		{
			string text = XDocument.Parse(Text("META-INF/container.xml")).Descendants().FirstOrDefault((XElement e) => e.Name.LocalName == "rootfile")?.Attribute("full-path")?.Value ?? throw new ShortTechException("The EPUB does not identify a readable package document.");
			XDocument xDocument = XDocument.Parse(Text(text));
			string text2 = (text.Contains('/') ? text.Substring(0, text.LastIndexOf('/') + 1) : "");
			Dictionary<string, string> dictionary = (from e in xDocument.Descendants()
				where e.Name.LocalName == "item"
				select e).ToDictionary((XElement e) => e.Attribute("id")?.Value ?? "", (XElement e) => e.Attribute("href")?.Value ?? "");
			string title = xDocument.Descendants().FirstOrDefault((XElement e) => e.Name.LocalName == "title")?.Value ?? Path.GetFileNameWithoutExtension(path);
			Doc doc = new Doc
			{
				Title = title
			};
			foreach (string item in (from e in xDocument.Descendants()
				where e.Name.LocalName == "itemref"
				select e.Attribute("idref")?.Value ?? "").ToList())
			{
				if (!dictionary.TryGetValue(item, out var value))
				{
					continue;
				}
				string text3 = Normalize(text2 + value);
				ZipArchiveEntry entry = zip.GetEntry(text3);
				if (entry == null)
				{
					continue;
				}
				string html = new StreamReader(entry.Open()).ReadToEnd();
				string chapterFolder = (text3.Contains('/') ? text3.Substring(0, text3.LastIndexOf('/') + 1) : "");
				if (doc.Blocks.Count > 0)
				{
					doc.Blocks.Add(new PageBreak());
				}
				HtmlReader.Read(html, title, delegate(string src)
				{
					ZipArchiveEntry entry2 = zip.GetEntry(Normalize(chapterFolder + src));
					if (entry2 == null)
					{
						return ((byte[] Data, string Ext)?)null;
					}
					using Stream stream = entry2.Open();
					using MemoryStream memoryStream = new MemoryStream();
					stream.CopyTo(memoryStream);
					return (memoryStream.ToArray(), (Path.GetExtension(src).TrimStart('.').ToLowerInvariant() == "png") ? "png" : ((Path.GetExtension(src).TrimStart('.').ToLowerInvariant() == "gif") ? "gif" : "jpg"));
				}, doc);
			}
			if (doc.Blocks.Count == 0)
			{
				throw new ShortTechException("The EPUB has no readable chapters.");
			}
			return doc;
		}
		finally
		{
			if (zip != null)
			{
				((IDisposable)zip).Dispose();
			}
		}
		string Text(string name)
		{
			return new StreamReader(zip.GetEntry(name)?.Open() ?? throw new ShortTechException("The EPUB does not identify a readable package document.")).ReadToEnd();
		}
	}

	private static string Normalize(string path)
	{
		List<string> list = new List<string>();
		string[] array = path.Replace('\\', '/').Split('/');
		foreach (string text in array)
		{
			if (text == "..")
			{
				if (list.Count > 0)
				{
					list.RemoveAt(list.Count - 1);
				}
			}
			else if (text != "." && text.Length > 0)
			{
				list.Add(text);
			}
		}
		return string.Join('/', list);
	}

	public static void Write(Doc doc, string path)
	{
		List<Doc> list = new List<Doc>
		{
			new Doc
			{
				Title = doc.Title
			}
		};
		foreach (Block block in doc.Blocks)
		{
			if (block is PageBreak)
			{
				list.Add(new Doc
				{
					Title = doc.Title
				});
			}
			else
			{
				list[list.Count - 1].Blocks.Add(block);
			}
		}
		list.RemoveAll((Doc chapter) => chapter.Blocks.Count == 0);
		if (list.Count == 0)
		{
			list.Add(new Doc
			{
				Title = doc.Title
			});
		}
		bool flag = Bidi.IsRightToLeft(doc.PlainText);
		string value = (flag ? "ar" : "en");
		string value2 = $"urn:uuid:{Guid.NewGuid()}";
		List<(string Name, Picture Picture)> images = new List<(string, Picture)>();
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		using ZipArchive zipArchive = ZipFile.Open(path, ZipArchiveMode.Create);
		using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open()))
		{
			streamWriter.Write("application/epub+zip");
		}
		Put(zipArchive, "META-INF/container.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n  <rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles>\n</container>");
		Put(zipArchive, "OEBPS/style.css", DocText.Css(forPrint: false).Replace("max-width: 46em; margin: 2em auto; padding: 0 1em;", "margin: 0 5%;"));
		for (int num = 0; num < list.Count; num++)
		{
			StringBuilder stringBuilder = new StringBuilder();
			DocText.Body(list[num], stringBuilder, delegate(Picture picture2)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 2);
				defaultInterpolatedStringHandler.AppendLiteral("images/image");
				defaultInterpolatedStringHandler.AppendFormatted(images.Count + 1);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				string extension = picture2.Extension;
				bool flag2 = ((extension == "png" || extension == "gif") ? true : false);
				defaultInterpolatedStringHandler.AppendFormatted(flag2 ? picture2.Extension : "jpg");
				string text3 = defaultInterpolatedStringHandler.ToStringAndClear();
				images.Add((text3, picture2));
				return text3;
			});
			string text = list[num].Blocks.OfType<Heading>().FirstOrDefault()?.Text ?? $"{doc.Title} {num + 1}";
			Put(zipArchive, $"OEBPS/chapter{num + 1}.xhtml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE html>\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xml:lang=\"{value}\" lang=\"{value}\" dir=\"{(flag ? "rtl" : "ltr")}\">\n<head><meta charset=\"utf-8\"/><title>{DocText.Escape(text)}</title><link rel=\"stylesheet\" href=\"style.css\"/></head>\n<body>\n{Xhtml(stringBuilder.ToString())}\n</body>\n</html>");
		}
		foreach (var (text2, picture) in images)
		{
			using Stream stream = zipArchive.CreateEntry("OEBPS/" + text2, CompressionLevel.NoCompression).Open();
			stream.Write(picture.Data);
		}
		string value3 = string.Join("\n", list.Select((Doc chapter, int i) => $"<li><a href=\"chapter{i + 1}.xhtml\">{DocText.Escape(chapter.Blocks.OfType<Heading>().FirstOrDefault()?.Text ?? $"{doc.Title} {i + 1}")}</a></li>"));
		Put(zipArchive, "OEBPS/nav.xhtml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE html>\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\" xml:lang=\"{value}\" lang=\"{value}\" dir=\"{(flag ? "rtl" : "ltr")}\">\n<head><meta charset=\"utf-8\"/><title>{DocText.Escape(doc.Title)}</title></head>\n<body><nav epub:type=\"toc\" id=\"toc\"><h1>{DocText.Escape(doc.Title)}</h1><ol>\n{value3}\n</ol></nav></body>\n</html>");
		StringBuilder stringBuilder2 = new StringBuilder("<item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>\n<item id=\"css\" href=\"style.css\" media-type=\"text/css\"/>\n");
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder stringBuilder4 = stringBuilder3;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(71, 2, stringBuilder3);
			handler.AppendLiteral("<item id=\"c");
			handler.AppendFormatted(num2 + 1);
			handler.AppendLiteral("\" href=\"chapter");
			handler.AppendFormatted(num2 + 1);
			handler.AppendLiteral(".xhtml\" media-type=\"application/xhtml+xml\"/>\n");
			stringBuilder4.Append(ref handler);
		}
		for (int num3 = 0; num3 < images.Count; num3++)
		{
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder stringBuilder5 = stringBuilder3;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(39, 3, stringBuilder3);
			handler.AppendLiteral("<item id=\"img");
			handler.AppendFormatted(num3 + 1);
			handler.AppendLiteral("\" href=\"");
			handler.AppendFormatted(images[num3].Name);
			handler.AppendLiteral("\" media-type=\"");
			handler.AppendFormatted(images[num3].Name.EndsWith(".png") ? "image/png" : (images[num3].Name.EndsWith(".gif") ? "image/gif" : "image/jpeg"));
			handler.AppendLiteral("\"/>\n");
			stringBuilder5.Append(ref handler);
		}
		string value4 = string.Concat(from i in Enumerable.Range(1, list.Count)
			select $"<itemref idref=\"c{i}\"/>");
		Put(zipArchive, "OEBPS/content.opf", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"bookid\" xml:lang=\"{value}\"{(flag ? " dir=\"rtl\"" : "")}>\n  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n    <dc:identifier id=\"bookid\">{value2}</dc:identifier>\n    <dc:title>{DocText.Escape(doc.Title)}</dc:title>\n    <dc:language>{value}</dc:language>\n    <meta property=\"dcterms:modified\">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>\n  </metadata>\n  <manifest>\n{stringBuilder2}  </manifest>\n  <spine{(flag ? " page-progression-direction=\"rtl\"" : "")}>{value4}</spine>\n</package>");
	}

	private static string Xhtml(string html)
	{
		return Regex.Replace(html.Replace("<br>", "<br/>"), "<img([^>]*?)(?<!/)>", "<img$1/>");
	}

	private static void Put(ZipArchive zip, string name, string text)
	{
		using StreamWriter streamWriter = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		streamWriter.Write(text);
	}
}
