using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ShortTech.Core.Documents;

namespace ShortTech.Core;

public static class QrContent
{
	public enum Security
	{
		Wpa,
		Wep,
		None
	}

	public static readonly HashSet<string> FileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "txt", "md", "markdown", "url", "vcf", "webloc" };

	public static bool CanEncode(IReadOnlyList<string> files)
	{
		if (files.Count == 1)
		{
			return FileExtensions.Contains(FileKinds.Ext(files[0]));
		}
		return false;
	}

	public static string? Text(string file)
	{
		try
		{
			string text = FileKinds.Ext(file);
			if (!(text == "url"))
			{
				if (text == "webloc")
				{
					return (XDocument.Load(file).Descendants("key").FirstOrDefault((XElement k) => k.Value == "URL")?.ElementsAfterSelf("string").FirstOrDefault())?.Value;
				}
				return DocumentOps.ReadText(file).Trim();
			}
			return File.ReadAllLines(file).FirstOrDefault((string line) => line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))?.Substring(4);
		}
		catch (Exception ex) when (((ex is IOException || ex is XmlException) ? 1 : 0) != 0)
		{
			return null;
		}
	}

	public static string Title(Security security)
	{
		return security switch
		{
			Security.None => "None", 
			Security.Wpa => "WPA / WPA2 / WPA3", 
			_ => "WEP", 
		};
	}

	public static string Wifi(string name, string password, Security security, bool hidden)
	{
		string value = security switch
		{
			Security.Wpa => "WPA", 
			Security.Wep => "WEP", 
			_ => "nopass", 
		};
		string text = $"WIFI:T:{value};S:{Escape(name)};";
		if (security != Security.None)
		{
			text = text + "P:" + Escape(password) + ";";
		}
		if (hidden)
		{
			text += "H:true;";
		}
		return text + ";";
		static string Escape(string source)
		{
			return string.Concat(source.Select((char c) => "\\;,:\"".Contains(c) ? $"\\{c}" : c.ToString()));
		}
	}

	public static async Task<IReadOnlyList<string>> Quick(IReadOnlyList<string> files, OperationContext context)
	{
		string text = files[0];
		string text2 = Text(text);
		if (string.IsNullOrEmpty(text2))
		{
			throw new ShortTechException("No text found");
		}
		QrMatrix matrix = QrMatrix.Encode(text2, QrMatrix.Correction.Medium) ?? throw new ShortTechException("This is too long for a QR code. Shorten it or lower the error correction.");
		string output = Output.PathFor(text, "png", "QR");
		await Task.Run(delegate
		{
			Sta.Run(delegate
			{
				File.WriteAllBytes(output, QrDesign.Png(new QrDesign().Image(matrix, 1024)));
			});
		});
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
	}
}
