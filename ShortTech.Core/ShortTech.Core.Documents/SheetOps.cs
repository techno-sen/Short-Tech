using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClosedXML.Excel;
using ExcelDataReader;

namespace ShortTech.Core.Documents;

public static class SheetOps
{
	public static readonly IReadOnlyList<string> Targets = new global::_003C_003Ez__ReadOnlyArray<string>(new string[6] { "xlsx", "ods", "csv", "tsv", "pdf", "html" });

	private static readonly XNamespace OfficeNs = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

	private static readonly XNamespace TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

	private static readonly XNamespace TextNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

	public static string Title(string target)
	{
		return target.ToUpperInvariant();
	}

	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, string target, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Converting {index + 1} of {files.Count}");
			string file = files[index];
			List<Sheet> sheets = await Task.Run(() => Read(file));
			if (sheets.Count == 0)
			{
				throw new ShortTechException(Path.GetFileName(file) + " has no readable sheet.");
			}
			string output = Output.PathFor(file, target);
			switch (target)
			{
			case "xlsx":
				await Task.Run(delegate
				{
					WriteXlsx(sheets, output);
				});
				break;
			case "ods":
				await Task.Run(delegate
				{
					WriteOds(sheets, output);
				});
				break;
			case "csv":
			case "tsv":
				await File.WriteAllTextAsync(output, Delimited(sheets[0], (target == "csv") ? ',' : '\t'), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
				break;
			case "html":
				await File.WriteAllTextAsync(output, Html(sheets, Path.GetFileNameWithoutExtension(file), forPrint: false), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				break;
			case "pdf":
				await PdfPrinter.Print(Html(sheets, Path.GetFileNameWithoutExtension(file), forPrint: true), output, sheets.Any((Sheet s) => s.Rows.Any((List<object> r) => r.Count > 6)));
				break;
			}
			outputs.Add(output);
		}
		return outputs;
	}

	public static List<Sheet> Read(string path)
	{
		switch (FileKinds.Ext(path))
		{
		case "xlsx":
			return ReadXlsx(path);
		case "xls":
			return ReadXls(path);
		case "ods":
			return ReadOds(path);
		case "csv":
		{
			int num = 1;
			List<Sheet> list2 = new List<Sheet>(num);
			CollectionsMarshal.SetCount(list2, num);
			CollectionsMarshal.AsSpan(list2)[0] = ReadDelimited(path, ',');
			return list2;
		}
		case "tsv":
		{
			int num = 1;
			List<Sheet> list = new List<Sheet>(num);
			CollectionsMarshal.SetCount(list, num);
			CollectionsMarshal.AsSpan(list)[0] = ReadDelimited(path, '\t');
			return list;
		}
		case "numbers":
			throw new ShortTechException("Pages, Keynote and Numbers files are not supported on Windows.");
		default:
			throw new ShortTechException(Path.GetFileName(path) + " is not a spreadsheet.");
		}
	}

	private static List<Sheet> ReadXlsx(string path)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook(path);
		return xLWorkbook.Worksheets.Select(delegate(IXLWorksheet sheet)
		{
			IXLRange iXLRange = sheet.RangeUsed();
			List<List<object>> list = new List<List<object>>();
			if (iXLRange != null)
			{
				foreach (IXLRangeRow item in iXLRange.Rows())
				{
					list.Add((from cell in item.Cells()
						select cell.Value.Type switch
						{
							XLDataType.Number => (object)cell.Value.GetNumber(),
							XLDataType.Boolean => (object)cell.Value.GetBoolean(),
							XLDataType.DateTime => (object)cell.Value.GetDateTime(),
							XLDataType.Blank => null,
							_ => (object)cell.GetFormattedString(),
						}).ToList());
				}
			}
			return new Sheet(sheet.Name, list);
		}).ToList();
	}

	private static List<Sheet> ReadXls(string path)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		using FileStream fileStream = File.OpenRead(path);
		using IExcelDataReader excelDataReader = ExcelReaderFactory.CreateReader(fileStream);
		List<Sheet> list = new List<Sheet>();
		do
		{
			List<List<object>> list2 = new List<List<object>>();
			while (excelDataReader.Read())
			{
				List<object> list3 = new List<object>();
				for (int i = 0; i < excelDataReader.FieldCount; i++)
				{
					list3.Add(excelDataReader.GetValue(i));
				}
				list2.Add(list3);
			}
			list.Add(new Sheet(excelDataReader.Name ?? $"Sheet{list.Count + 1}", list2));
		}
		while (excelDataReader.NextResult());
		return list;
	}

	private static List<Sheet> ReadOds(string path)
	{
		using ZipArchive zipArchive = ZipFile.OpenRead(path);
		return XDocument.Load(zipArchive.GetEntry("content.xml").Open()).Descendants(TableNs + "table").Select(delegate(XElement table)
		{
			List<List<object>> list = new List<List<object>>();
			foreach (XElement item2 in table.Elements(TableNs + "table-row"))
			{
				int result;
				int num = Math.Min((!int.TryParse(item2.Attribute(TableNs + "number-rows-repeated")?.Value, out result)) ? 1 : result, 1000);
				List<object> list2 = new List<object>();
				foreach (XElement item3 in item2.Elements().Where(delegate(XElement e)
				{
					string localName = e.Name.LocalName;
					return (localName == "table-cell" || localName == "covered-table-cell") ? true : false;
				}))
				{
					int result2;
					int num2 = Math.Min((!int.TryParse(item3.Attribute(TableNs + "number-columns-repeated")?.Value, out result2)) ? 1 : result2, 1000);
					object obj;
					switch (item3.Attribute(OfficeNs + "value-type")?.Value)
					{
					case "float":
					case "percentage":
					case "currency":
					{
						obj = (double.TryParse(item3.Attribute(OfficeNs + "value")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3) ? ((object)result3) : null);
						break;
					}
					case "boolean":
						obj = item3.Attribute(OfficeNs + "boolean-value")?.Value == "true";
						break;
					case null:
						obj = null;
						break;
					default:
						obj = string.Join("\n", from p in item3.Elements(TextNs + "p")
							select p.Value);
						break;
					}
					object item = obj;
					for (int num3 = 0; num3 < num2; num3++)
					{
						list2.Add(item);
					}
				}
				while (list2.Count > 0)
				{
					if (list2[list2.Count - 1] != null)
					{
						break;
					}
					list2.RemoveAt(list2.Count - 1);
				}
				for (int num4 = 0; num4 < num; num4++)
				{
					list.Add(list2);
				}
			}
			while (list.Count > 0)
			{
				if (list[list.Count - 1].Count != 0)
				{
					break;
				}
				list.RemoveAt(list.Count - 1);
			}
			return new Sheet(table.Attribute(TableNs + "name")?.Value ?? "Sheet1", list);
		})
			.ToList();
	}

	private static Sheet ReadDelimited(string path, char separator)
	{
		string text = DocumentOps.ReadText(path);
		if (separator == ',' && text.Count((char c2) => c2 == ';') > text.Count((char c2) => c2 == ',') * 2)
		{
			separator = ';';
		}
		List<List<object>> list = new List<List<object>>();
		List<object> list2 = new List<object>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		for (int num = 0; num < text.Length; num++)
		{
			char c = text[num];
			if (flag)
			{
				if (c == '"' && num + 1 < text.Length && text[num + 1] == '"')
				{
					stringBuilder.Append('"');
					num++;
				}
				else if (c == '"')
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			else if (c == '"' && stringBuilder.Length == 0)
			{
				flag = true;
			}
			else if (c == separator)
			{
				list2.Add(Typed(stringBuilder.ToString()));
				stringBuilder.Clear();
			}
			else if ((c == '\n' || c == '\r') ? true : false)
			{
				if (c == '\r' && num + 1 < text.Length && text[num + 1] == '\n')
				{
					num++;
				}
				list2.Add(Typed(stringBuilder.ToString()));
				stringBuilder.Clear();
				list.Add(list2);
				list2 = new List<object>();
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		if (stringBuilder.Length > 0 || list2.Count > 0)
		{
			list2.Add(Typed(stringBuilder.ToString()));
			list.Add(list2);
		}
		return new Sheet(Path.GetFileNameWithoutExtension(path), list);
	}

	private static object? Typed(string text)
	{
		if (text.Length == 0)
		{
			return null;
		}
		if (((text.Length < 16 && !text.StartsWith('0')) || text == "0" || text.StartsWith("0.")) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		return text;
	}

	private static void WriteXlsx(List<Sheet> sheets, string path)
	{
		using XLWorkbook xLWorkbook = new XLWorkbook();
		foreach (Sheet sheet in sheets)
		{
			string text = new string(sheet.Name.Where((char c) => !"[]*?/\\:".Contains(c)).ToArray());
			if (text.Length == 0)
			{
				text = $"Sheet{xLWorkbook.Worksheets.Count + 1}";
			}
			if (text.Length > 31)
			{
				text = text.Substring(0, 31);
			}
			while (xLWorkbook.Worksheets.Contains(text))
			{
				text = text.Substring(0, Math.Min(text.Length, 28)) + xLWorkbook.Worksheets.Count;
			}
			IXLWorksheet iXLWorksheet = xLWorkbook.Worksheets.Add(text);
			for (int num = 0; num < sheet.Rows.Count; num++)
			{
				for (int num2 = 0; num2 < sheet.Rows[num].Count; num2++)
				{
					IXLCell iXLCell = iXLWorksheet.Cell(num + 1, num2 + 1);
					object obj = sheet.Rows[num][num2];
					if (!(obj is double num3))
					{
						if (!(obj is int num4))
						{
							if (!(obj is bool flag))
							{
								if (!(obj is DateTime dateTime))
								{
									if (obj != null)
									{
										iXLCell.Value = obj.ToString();
									}
								}
								else
								{
									iXLCell.Value = dateTime;
								}
							}
							else
							{
								iXLCell.Value = flag;
							}
						}
						else
						{
							iXLCell.Value = num4;
						}
					}
					else
					{
						iXLCell.Value = num3;
					}
				}
			}
			if (sheet.Rows.SelectMany((List<object> r) => r).OfType<string>().Any(Bidi.IsRightToLeft))
			{
				iXLWorksheet.RightToLeft = true;
			}
			iXLWorksheet.Columns().AdjustToContents(1, Math.Min(200, sheet.Rows.Count));
		}
		xLWorkbook.SaveAs(path);
	}

	private static void WriteOds(List<Sheet> sheets, string path)
	{
		XElement content = new XElement(OfficeNs + "spreadsheet", sheets.Select((Sheet sheet) => new XElement(TableNs + "table", new XAttribute(TableNs + "name", sheet.Name), sheet.Rows.Select((List<object> row) => new XElement(TableNs + "table-row", row.Select(delegate(object value)
		{
			if (value is double num)
			{
				return new XElement(TableNs + "table-cell", new XAttribute(OfficeNs + "value-type", "float"), new XAttribute(OfficeNs + "value", num.ToString(CultureInfo.InvariantCulture)), new XElement(TextNs + "p", num.ToString(CultureInfo.InvariantCulture)));
			}
			return (value == null) ? new XElement(TableNs + "table-cell") : new XElement(TableNs + "table-cell", new XAttribute(OfficeNs + "value-type", "string"), new XElement(TextNs + "p", value.ToString()));
		}))))));
		XDocument xDocument = new XDocument(new XElement(OfficeNs + "document-content", new XAttribute(XNamespace.Xmlns + "office", OfficeNs), new XAttribute(XNamespace.Xmlns + "table", TableNs), new XAttribute(XNamespace.Xmlns + "text", TextNs), new XAttribute(OfficeNs + "version", "1.3"), new XElement(OfficeNs + "body", content)));
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		using ZipArchive zipArchive = ZipFile.Open(path, ZipArchiveMode.Create);
		using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open()))
		{
			streamWriter.Write("application/vnd.oasis.opendocument.spreadsheet");
		}
		using (Stream stream = zipArchive.CreateEntry("content.xml").Open())
		{
			xDocument.Save(stream);
		}
		using StreamWriter streamWriter2 = new StreamWriter(zipArchive.CreateEntry("META-INF/manifest.xml").Open());
		streamWriter2.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<manifest:manifest xmlns:manifest=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\" manifest:version=\"1.3\">\n <manifest:file-entry manifest:full-path=\"/\" manifest:media-type=\"application/vnd.oasis.opendocument.spreadsheet\"/>\n <manifest:file-entry manifest:full-path=\"content.xml\" manifest:media-type=\"text/xml\"/>\n</manifest:manifest>");
	}

	private static string Delimited(Sheet sheet, char separator)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (List<object> row in sheet.Rows)
		{
			stringBuilder.AppendJoin(separator, row.Select(delegate(object value)
			{
				string text = ((value is double num) ? num.ToString(CultureInfo.InvariantCulture) : ((value != null) ? (value.ToString() ?? "") : ""));
				string text2 = text;
				char[] obj = new char[4] { '\0', '"', '\n', '\r' };
				obj[0] = separator;
				return (text2.IndexOfAny(obj) < 0) ? text2 : ("\"" + text2.Replace("\"", "\"\"") + "\"");
			}));
			stringBuilder.Append("\r\n");
		}
		return stringBuilder.ToString();
	}

	private static string Html(List<Sheet> sheets, string title, bool forPrint)
	{
		bool flag = sheets.SelectMany((Sheet s) => s.Rows).SelectMany((List<object> r) => r).OfType<string>()
			.Any(Bidi.IsRightToLeft);
		StringBuilder stringBuilder = new StringBuilder($"<!DOCTYPE html><html dir=\"{(flag ? "rtl" : "ltr")}\"><head><meta charset=\"utf-8\"><title>{DocText.Escape(title)}</title><style>");
		stringBuilder.Append(forPrint ? "@page { margin: 0.5in; } " : "");
		stringBuilder.Append("body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 10pt; } h2 { font-size: 12pt; margin: 1em 0 0.4em; } ");
		stringBuilder.Append("table { border-collapse: collapse; margin-bottom: 1.5em; } td { border: 1px solid #c8c8cc; padding: 3px 6px; white-space: nowrap; } ");
		stringBuilder.Append("td.n { text-align: end; font-variant-numeric: tabular-nums; } tr:first-child td { background: #f1f1f4; font-weight: 600; } .break { page-break-after: always; }");
		stringBuilder.Append("</style></head><body>");
		for (int num = 0; num < sheets.Count; num++)
		{
			if (sheets.Count > 1)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2);
				handler.AppendLiteral("<h2>");
				handler.AppendFormatted(DocText.Escape(sheets[num].Name));
				handler.AppendLiteral("</h2>");
				stringBuilder2.Append(ref handler);
			}
			stringBuilder.Append("<table>");
			foreach (List<object> row in sheets[num].Rows)
			{
				stringBuilder.Append("<tr>").Append(string.Concat(row.Select((object v) => (!(v is double num2)) ? ("<td>" + DocText.Escape(v?.ToString() ?? "") + "</td>") : ("<td class=\"n\">" + num2.ToString(CultureInfo.CurrentCulture) + "</td>")))).Append("</tr>");
			}
			stringBuilder.Append("</table>");
			if (forPrint && num < sheets.Count - 1)
			{
				stringBuilder.Append("<div class=\"break\"></div>");
			}
		}
		return stringBuilder.Append("</body></html>").ToString();
	}
}
