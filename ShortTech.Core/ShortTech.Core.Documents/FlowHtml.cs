using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace ShortTech.Core.Documents;

public static class FlowHtml
{
	public static string From(FlowDocument document)
	{
		StringBuilder stringBuilder = new StringBuilder("<html><body>");
		double baseSize = (from p in document.Blocks.OfType<System.Windows.Documents.Paragraph>()
			select p.FontSize).DefaultIfEmpty(12.0).Min();
		foreach (System.Windows.Documents.Block block in document.Blocks)
		{
			Block(block, stringBuilder, baseSize);
		}
		return stringBuilder.Append("</body></html>").ToString();
	}

	private static void Block(System.Windows.Documents.Block block, StringBuilder html, double baseSize)
	{
		if (!(block is System.Windows.Documents.Paragraph paragraph))
		{
			if (!(block is List { MarkerStyle: var markerStyle } list))
			{
				if (!(block is System.Windows.Documents.Table table))
				{
					if (!(block is Section section))
					{
						return;
					}
					{
						foreach (System.Windows.Documents.Block block2 in section.Blocks)
						{
							Block(block2, html, baseSize);
						}
						return;
					}
				}
				html.Append("<table>");
				foreach (TableRowGroup item in (IEnumerable<TableRowGroup>)table.RowGroups)
				{
					foreach (TableRow item2 in (IEnumerable<TableRow>)item.Rows)
					{
						html.Append("<tr>");
						foreach (TableCell item3 in (IEnumerable<TableCell>)item2.Cells)
						{
							html.Append("<td>");
							foreach (System.Windows.Documents.Paragraph item4 in item3.Blocks.OfType<System.Windows.Documents.Paragraph>())
							{
								foreach (Inline inline in item4.Inlines)
								{
									Inline(inline, html);
								}
							}
							html.Append("</td>");
						}
						html.Append("</tr>");
					}
				}
				html.Append("</table>");
				return;
			}
			bool flag = (uint)(markerStyle - 5) <= 4u;
			string value = (flag ? "ol" : "ul");
			html.Append('<').Append(value).Append('>');
			foreach (System.Windows.Documents.ListItem listItem in list.ListItems)
			{
				html.Append("<li>");
				foreach (System.Windows.Documents.Block block3 in listItem.Blocks)
				{
					if (block3 is System.Windows.Documents.Paragraph paragraph2)
					{
						foreach (Inline inline2 in paragraph2.Inlines)
						{
							Inline(inline2, html);
						}
					}
					else
					{
						Block(block3, html, baseSize);
					}
				}
				html.Append("</li>");
			}
			html.Append("</").Append(value).Append('>');
			return;
		}
		double num = (from r in paragraph.Inlines.OfType<Run>()
			select r.FontSize).DefaultIfEmpty(paragraph.FontSize).Max();
		string value2 = ((num >= baseSize * 1.7) ? "h1" : ((num >= baseSize * 1.4) ? "h2" : ((num >= baseSize * 1.2) ? "h3" : "p")));
		html.Append('<').Append(value2).Append('>');
		foreach (Inline inline3 in paragraph.Inlines)
		{
			Inline(inline3, html);
		}
		html.Append("</").Append(value2).Append('>');
	}

	private static void Inline(Inline inline, StringBuilder html)
	{
		if (!(inline is Run run))
		{
			if (!(inline is LineBreak))
			{
				if (!(inline is Hyperlink hyperlink))
				{
					if (!(inline is System.Windows.Documents.Span span))
					{
						return;
					}
					bool flag = span is Bold || span.FontWeight.ToOpenTypeWeight() >= 600;
					bool flag2 = span is Italic || span.FontStyle == FontStyles.Italic;
					if (flag)
					{
						html.Append("<b>");
					}
					if (flag2)
					{
						html.Append("<i>");
					}
					foreach (Inline inline2 in span.Inlines)
					{
						Inline(inline2, html);
					}
					if (flag2)
					{
						html.Append("</i>");
					}
					if (flag)
					{
						html.Append("</b>");
					}
					return;
				}
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, html);
				handler.AppendLiteral("<a href=\"");
				handler.AppendFormatted(SecurityElement.Escape(hyperlink.NavigateUri?.ToString() ?? ""));
				handler.AppendLiteral("\">");
				html.Append(ref handler);
				foreach (Inline inline3 in hyperlink.Inlines)
				{
					Inline(inline3, html);
				}
				html.Append("</a>");
			}
			else
			{
				html.Append("<br>");
			}
		}
		else
		{
			string text = SecurityElement.Escape(run.Text) ?? "";
			if (run.FontWeight.ToOpenTypeWeight() >= 600)
			{
				text = "<b>" + text + "</b>";
			}
			if (run.FontStyle == FontStyles.Italic)
			{
				text = "<i>" + text + "</i>";
			}
			TextDecorationCollection textDecorations = run.TextDecorations;
			if (textDecorations != null && textDecorations.Any((TextDecoration d) => d.Location == TextDecorationLocation.Underline))
			{
				text = "<u>" + text + "</u>";
			}
			html.Append(text);
		}
	}

	public static string ToXaml(Doc doc)
	{
		StringBuilder stringBuilder = new StringBuilder("<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" FontFamily=\"Calibri, Arial\" FontSize=\"14.67\">");
		foreach (Block block in doc.Blocks)
		{
			if (!(block is Heading heading))
			{
				if (!(block is Paragraph paragraph))
				{
					if (!(block is Quote quote))
					{
						if (!(block is ListItem listItem))
						{
							if (!(block is CodeBlock codeBlock))
							{
								if (!(block is Table table))
								{
									if (block is PageBreak)
									{
										stringBuilder.Append("<Paragraph BreakPageBefore=\"True\"/>");
									}
									continue;
								}
								stringBuilder.Append("<Table CellSpacing=\"0\" BorderBrush=\"Gray\" BorderThickness=\"1\"><TableRowGroup>");
								foreach (List<List<Span>> row in table.Rows)
								{
									stringBuilder.Append("<TableRow>").Append(string.Concat(row.Select((List<Span> cell) => "<TableCell BorderBrush=\"Gray\" BorderThickness=\"0.5\" Padding=\"4\"><Paragraph>" + Spans(cell) + "</Paragraph></TableCell>"))).Append("</TableRow>");
								}
								stringBuilder.Append("</TableRowGroup></Table>");
							}
							else
							{
								StringBuilder stringBuilder2 = stringBuilder;
								StringBuilder stringBuilder3 = stringBuilder2;
								StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(45, 1, stringBuilder2);
								handler.AppendLiteral("<Paragraph FontFamily=\"Consolas\">");
								handler.AppendFormatted(SecurityElement.Escape(codeBlock.Code)?.Replace("\n", "<LineBreak/>"));
								handler.AppendLiteral("</Paragraph>");
								stringBuilder3.Append(ref handler);
							}
						}
						else
						{
							StringBuilder stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder4 = stringBuilder2;
							StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(88, 4, stringBuilder2);
							handler.AppendLiteral("<List MarkerStyle=\"");
							handler.AppendFormatted(listItem.Ordered ? "Decimal" : "Disc");
							handler.AppendLiteral("\" Margin=\"");
							handler.AppendFormatted(20 + listItem.Depth * 20);
							handler.AppendLiteral(",0,0,0\"><ListItem><Paragraph");
							handler.AppendFormatted(Dir(listItem.Text));
							handler.AppendLiteral(">");
							handler.AppendFormatted(Spans(listItem.Spans));
							handler.AppendLiteral("</Paragraph></ListItem></List>");
							stringBuilder4.Append(ref handler);
						}
					}
					else
					{
						StringBuilder stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder5 = stringBuilder2;
						StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(60, 2, stringBuilder2);
						handler.AppendLiteral("<Paragraph Margin=\"30,0,0,0\" FontStyle=\"Italic\"");
						handler.AppendFormatted(Dir(quote.Text));
						handler.AppendLiteral(">");
						handler.AppendFormatted(Spans(quote.Spans));
						handler.AppendLiteral("</Paragraph>");
						stringBuilder5.Append(ref handler);
					}
				}
				else
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder6 = stringBuilder2;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 2, stringBuilder2);
					handler.AppendLiteral("<Paragraph");
					handler.AppendFormatted(Dir(paragraph.Text));
					handler.AppendLiteral(">");
					handler.AppendFormatted(Spans(paragraph.Spans));
					handler.AppendLiteral("</Paragraph>");
					stringBuilder6.Append(ref handler);
				}
			}
			else
			{
				int value = heading.Level switch
				{
					1 => 26, 
					2 => 21, 
					3 => 18, 
					_ => 16, 
				};
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(53, 3, stringBuilder2);
				handler.AppendLiteral("<Paragraph FontSize=\"");
				handler.AppendFormatted(value);
				handler.AppendLiteral("\" FontWeight=\"Bold\"");
				handler.AppendFormatted(Dir(heading.Text));
				handler.AppendLiteral(">");
				handler.AppendFormatted(Spans(heading.Spans));
				handler.AppendLiteral("</Paragraph>");
				stringBuilder7.Append(ref handler);
			}
		}
		return stringBuilder.Append("</FlowDocument>").ToString();
		static string Dir(string text)
		{
			if (!Bidi.IsRightToLeft(text))
			{
				return "";
			}
			return " FlowDirection=\"RightToLeft\"";
		}
		static string Spans(IEnumerable<Span> spans)
		{
			return string.Concat(spans.Select(delegate(Span span)
			{
				string[] source = (SecurityElement.Escape(span.Text) ?? "").Split('\n');
				string text = string.Join("<LineBreak/>", source.Select((string p) => "<Run>" + p + "</Run>"));
				if (span.Bold)
				{
					text = "<Bold>" + text + "</Bold>";
				}
				if (span.Italic)
				{
					text = "<Italic>" + text + "</Italic>";
				}
				if (span.Underline)
				{
					text = "<Underline>" + text + "</Underline>";
				}
				return text;
			}));
		}
	}
}
