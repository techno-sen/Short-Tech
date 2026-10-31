using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace ShortTech.Core.Documents;

public static class Odt
{
	private static readonly XNamespace Office = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

	private static readonly XNamespace TextNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

	private static readonly XNamespace TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

	private static readonly XNamespace StyleNs = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";

	private static readonly XNamespace Fo = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";

	public static Doc Read(string path)
	{
		Doc doc;
		HashSet<string> bold;
		HashSet<string> italic;
		using (ZipArchive zipArchive = ZipFile.OpenRead(path))
		{
			XDocument xDocument = XDocument.Load((zipArchive.GetEntry("content.xml") ?? throw new ShortTechException(Path.GetFileName(path) + " could not be read as a document")).Open());
			doc = new Doc
			{
				Title = Path.GetFileNameWithoutExtension(path)
			};
			bold = (from s in xDocument.Descendants(StyleNs + "style")
				where s.Descendants().Any((XElement d) => d.Attribute(Fo + "font-weight")?.Value == "bold")
				select s.Attribute(StyleNs + "name")?.Value).ToHashSet();
			italic = (from s in xDocument.Descendants(StyleNs + "style")
				where s.Descendants().Any((XElement d) => d.Attribute(Fo + "font-style")?.Value == "italic")
				select s.Attribute(StyleNs + "name")?.Value).ToHashSet();
			XElement xElement = xDocument.Descendants(Office + "text").FirstOrDefault();
			if (xElement == null)
			{
				return doc;
			}
			foreach (XElement item2 in xElement.Elements())
			{
				Block(item2, 0, ordered: false);
			}
			return doc;
		}
		void Block(XElement e, int depth, bool ordered)
		{
			if (e.Name == TextNs + "h")
			{
				doc.Blocks.Add(new Heading((!int.TryParse(e.Attribute(TextNs + "outline-level")?.Value, out var result)) ? 1 : result, Spans(e)));
			}
			else if (e.Name == TextNs + "p")
			{
				List<Span> list = Spans(e);
				if (list.Any((Span x) => x.Text.Trim().Length > 0))
				{
					doc.Blocks.Add(new Paragraph(list));
				}
			}
			else
			{
				if (e.Name == TextNs + "list")
				{
					foreach (XElement item3 in e.Elements(TextNs + "list-item"))
					{
						foreach (XElement item4 in item3.Elements())
						{
							if (item4.Name == TextNs + "list")
							{
								Block(item4, depth + 1, ordered);
							}
							else
							{
								doc.Blocks.Add(new ListItem(Spans(item4), ordered, depth));
							}
						}
					}
					return;
				}
				if (!(e.Name == TableNs + "table"))
				{
					foreach (XElement item5 in e.Elements())
					{
						Block(item5, depth, ordered);
					}
					return;
				}
				List<List<List<Span>>> list2 = (from row in e.Descendants(TableNs + "table-row")
					select (from cell in row.Elements(TableNs + "table-cell")
						select cell.Elements().SelectMany(Spans).ToList()).ToList()).ToList();
				if (list2.Count > 0)
				{
					doc.Blocks.Add(new Table(list2));
				}
			}
		}
		List<Span> Spans(XElement element)
		{
			List<Span> spans = new List<Span>();
			foreach (XNode item6 in element.Nodes())
			{
				Walk(item6, b: false, i: false);
			}
			return spans;
		void Walk(XNode node, bool b, bool i)
		{
			if (node is XText xText)
			{
				spans.Add(new Span(xText.Value, b, i));
			}
			else if (node is XElement xElement2)
			{
				if (xElement2.Name == TextNs + "s")
				{
					spans.Add(new Span(new string(' ', (!int.TryParse(xElement2.Attribute(TextNs + "c")?.Value, out var result)) ? 1 : result)));
				}
				else if (xElement2.Name == TextNs + "tab")
				{
					spans.Add(new Span("\t"));
				}
				else
				{
					if (!(xElement2.Name == TextNs + "line-break"))
					{
						string item = xElement2.Attribute(TextNs + "style-name")?.Value;
						{
							foreach (XNode item7 in xElement2.Nodes())
							{
								Walk(item7, b || bold.Contains(item), i || italic.Contains(item));
							}
							return;
						}
					}
					spans.Add(new Span("\n"));
				}
			}
		}
		}
	}

	public static void Write(Doc doc, string path)
	{
		bool flag = Bidi.IsRightToLeft(doc.PlainText);
		XElement xElement = new XElement(Office + "text");
		XElement xElement2 = null;
		bool? flag2 = null;
		foreach (Block block in doc.Blocks)
		{
			if (block is ListItem listItem)
			{
				if (xElement2 == null || flag2 != listItem.Ordered)
				{
					xElement2 = new XElement(TextNs + "list", new XAttribute(TextNs + "style-name", listItem.Ordered ? "Numbers" : "Bullets"));
					xElement.Add(xElement2);
					flag2 = listItem.Ordered;
				}
				xElement2.Add(new XElement(TextNs + "list-item", Inline(new XElement(TextNs + "p", new XAttribute(TextNs + "style-name", flag ? "RTL" : "Standard")), listItem.Spans)));
				continue;
			}
			xElement2 = null;
			if (!(block is Heading heading))
			{
				if (!(block is Paragraph paragraph))
				{
					if (!(block is Quote quote))
					{
						if (!(block is CodeBlock codeBlock))
						{
							if (!(block is Table table))
							{
								if (block is PageBreak)
								{
									xElement.Add(new XElement(TextNs + "p", new XAttribute(TextNs + "style-name", "Break")));
								}
								continue;
							}
							int num = table.Rows.Max((List<List<Span>> r) => r.Count);
							XElement xElement3 = new XElement(TableNs + "table", new XElement(TableNs + "table-column", new XAttribute(TableNs + "number-columns-repeated", num)));
							foreach (List<List<Span>> row in table.Rows)
							{
								xElement3.Add(new XElement(TableNs + "table-row", row.Select((List<Span> cell) => new XElement(TableNs + "table-cell", Inline(new XElement(TextNs + "p"), cell)))));
							}
							xElement.Add(xElement3);
						}
						else
						{
							string[] array = codeBlock.Code.TrimEnd().Split('\n');
							foreach (string text in array)
							{
								xElement.Add(new XElement(TextNs + "p", new XAttribute(TextNs + "style-name", "Code"), text.TrimEnd('\r')));
							}
						}
					}
					else
					{
						xElement.Add(Inline(new XElement(TextNs + "p", new XAttribute(TextNs + "style-name", "Quotations")), quote.Spans));
					}
				}
				else
				{
					xElement.Add(Inline(new XElement(TextNs + "p", new XAttribute(TextNs + "style-name", flag ? "RTL" : "Standard")), paragraph.Spans));
				}
			}
			else
			{
				xElement.Add(Inline(new XElement(TextNs + "h", new XAttribute(TextNs + "style-name", $"Heading_20_{Math.Clamp(heading.Level, 1, 6)}"), new XAttribute(TextNs + "outline-level", Math.Clamp(heading.Level, 1, 6))), heading.Spans));
			}
		}
		XDocument xDocument = new XDocument(new XElement(Office + "document-content", new XAttribute(XNamespace.Xmlns + "office", Office), new XAttribute(XNamespace.Xmlns + "text", TextNs), new XAttribute(XNamespace.Xmlns + "table", TableNs), new XAttribute(XNamespace.Xmlns + "style", StyleNs), new XAttribute(XNamespace.Xmlns + "fo", Fo), new XAttribute(Office + "version", "1.3"), new XElement(Office + "automatic-styles", new XElement(StyleNs + "style", new XAttribute(StyleNs + "name", "B"), new XAttribute(StyleNs + "family", "text"), new XElement(StyleNs + "text-properties", new XAttribute(Fo + "font-weight", "bold"))), new XElement(StyleNs + "style", new XAttribute(StyleNs + "name", "I"), new XAttribute(StyleNs + "family", "text"), new XElement(StyleNs + "text-properties", new XAttribute(Fo + "font-style", "italic"))), new XElement(StyleNs + "style", new XAttribute(StyleNs + "name", "BI"), new XAttribute(StyleNs + "family", "text"), new XElement(StyleNs + "text-properties", new XAttribute(Fo + "font-weight", "bold"), new XAttribute(Fo + "font-style", "italic"))), new XElement(StyleNs + "style", new XAttribute(StyleNs + "name", "RTL"), new XAttribute(StyleNs + "family", "paragraph"), new XAttribute(StyleNs + "parent-style-name", "Standard"), new XElement(StyleNs + "paragraph-properties", new XAttribute(StyleNs + "writing-mode", "rl-tb"), new XAttribute(Fo + "text-align", "end"))), new XElement(StyleNs + "style", new XAttribute(StyleNs + "name", "Break"), new XAttribute(StyleNs + "family", "paragraph"), new XElement(StyleNs + "paragraph-properties", new XAttribute(Fo + "break-after", "page")))), new XElement(Office + "body", xElement)));
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		using (ZipArchive zipArchive = ZipFile.Open(path, ZipArchiveMode.Create))
		{
			using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open()))
			{
				streamWriter.Write("application/vnd.oasis.opendocument.text");
			}
			using (Stream stream = zipArchive.CreateEntry("content.xml").Open())
			{
				xDocument.Save(stream);
			}
			using StreamWriter streamWriter2 = new StreamWriter(zipArchive.CreateEntry("META-INF/manifest.xml").Open());
			streamWriter2.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\" manifest:version=\"1.3\">\n <manifest:file-entry manifest:full-path=\"/\" manifest:media-type=\"application/vnd.oasis.opendocument.text\"/>\n <manifest:file-entry manifest:full-path=\"content.xml\" manifest:media-type=\"text/xml\"/>\n</manifest:manifest>");
		}
		static XElement Inline(XElement into, IEnumerable<Span> spans)
		{
			foreach (Span span in spans)
			{
				string text2 = ((span.Bold && span.Italic) ? "BI" : (span.Bold ? "B" : (span.Italic ? "I" : null)));
				string[] array2 = span.Text.Split('\n');
				for (int i = 0; i < array2.Length; i++)
				{
					if (i > 0)
					{
						into.Add(new XElement(TextNs + "line-break"));
					}
					if (text2 == null)
					{
						into.Add(array2[i]);
					}
					else
					{
						into.Add(new XElement(TextNs + "span", new XAttribute(TextNs + "style-name", text2), array2[i]));
					}
				}
			}
			return into;
		}
	}
}
