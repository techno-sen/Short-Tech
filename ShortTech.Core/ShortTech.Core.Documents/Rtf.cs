using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ShortTech.Core.Documents;

public static class Rtf
{
	public static Doc Read(string path)
	{
		string html = null;
		Sta.Run(delegate
		{
			FlowDocument flowDocument = new FlowDocument();
			TextRange textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
			using FileStream stream = File.OpenRead(path);
			textRange.Load(stream, DataFormats.Rtf);
			html = FlowHtml.From(flowDocument);
		});
		return HtmlReader.Read(html ?? "", Path.GetFileNameWithoutExtension(path));
	}

	public static void Write(Doc doc, string path)
	{
		string xaml = FlowHtml.ToXaml(doc);
		Sta.Run(delegate
		{
			FlowDocument flowDocument = (FlowDocument)XamlReader.Parse(xaml);
			TextRange textRange = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
			using FileStream stream = File.Create(path);
			textRange.Save(stream, DataFormats.Rtf);
		});
	}
}
