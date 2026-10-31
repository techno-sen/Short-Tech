using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ShortTech.Core.Documents;

public static class DocText
{
	public static string Html(Doc doc, bool forPrint = false, Func<Picture, string>? pictureSource = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = Bidi.IsRightToLeft(doc.PlainText);
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(68, 2, stringBuilder2);
		handler.AppendLiteral("<!DOCTYPE html>\n<html lang=\"");
		handler.AppendFormatted(flag ? "ar" : "en");
		handler.AppendLiteral("\" dir=\"");
		handler.AppendFormatted(flag ? "rtl" : "ltr");
		handler.AppendLiteral("\">\n<head>\n<meta charset=\"utf-8\">\n");
		stringBuilder3.Append(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(49, 2, stringBuilder2);
		handler.AppendLiteral("<title>");
		handler.AppendFormatted(Escape(doc.Title));
		handler.AppendLiteral("</title>\n<style>\n");
		handler.AppendFormatted(Css(forPrint));
		handler.AppendLiteral("\n</style>\n</head>\n<body>\n");
		stringBuilder4.Append(ref handler);
		Body(doc, stringBuilder, pictureSource);
		stringBuilder.Append("</body>\n</html>\n");
		return stringBuilder.ToString();
	}

	public static string Css(bool forPrint)
	{
		return (forPrint ? "@page { size: Letter; margin: 1in; }\n" : "body { max-width: 46em; margin: 2em auto; padding: 0 1em; }\n") + "body { font-family: 'Segoe UI', 'Noto Naskh Arabic', 'Arial', sans-serif; font-size: 11pt; line-height: 1.55; color: #1b1b1b; }\nh1, h2, h3, h4 { line-height: 1.25; margin: 1.1em 0 0.4em; }\ntable { border-collapse: collapse; margin: 0.8em 0; } td, th { border: 1px solid #bbb; padding: 4px 8px; vertical-align: top; }\nth { background: #f1f1f4; } img { max-width: 100%; height: auto; } pre, code { font-family: Consolas, monospace; font-size: 0.92em; }\npre { background: #f5f5f7; padding: 0.8em; overflow: auto; white-space: pre-wrap; } blockquote { border-inline-start: 3px solid #ccc; margin: 0.8em 0; padding-inline-start: 1em; color: #555; }\n.break { page-break-after: always; break-after: page; }";
	}

	public static void Body(Doc doc, StringBuilder html, Func<Picture, string>? pictureSource = null)
	{
		bool? flag = null;
		foreach (Block block in doc.Blocks)
		{
			if (block is ListItem listItem)
			{
				if (flag != listItem.Ordered)
				{
					if (flag.HasValue)
					{
						html.Append((flag == true) ? "</ol>\n" : "</ul>\n");
					}
					html.Append(listItem.Ordered ? "<ol>\n" : "<ul>\n");
					flag = listItem.Ordered;
				}
				StringBuilder stringBuilder = html;
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, stringBuilder);
				handler.AppendLiteral("<li");
				handler.AppendFormatted(Dir(listItem.Text));
				handler.AppendLiteral(">");
				handler.AppendFormatted(Inline(listItem.Spans));
				handler.AppendLiteral("</li>\n");
				stringBuilder2.Append(ref handler);
				continue;
			}
			if (flag.HasValue)
			{
				html.Append((flag == true) ? "</ol>\n" : "</ul>\n");
				flag = null;
			}
			if (!(block is Heading heading))
			{
				if (!(block is Paragraph paragraph))
				{
					if (!(block is Table table))
					{
						if (!(block is Picture picture))
						{
							if (!(block is CodeBlock codeBlock))
							{
								if (!(block is Quote quote))
								{
									if (block is PageBreak)
									{
										html.Append("<div class=\"break\"></div>\n");
									}
									continue;
								}
								StringBuilder stringBuilder = html;
								StringBuilder stringBuilder3 = stringBuilder;
								StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(26, 2, stringBuilder);
								handler.AppendLiteral("<blockquote");
								handler.AppendFormatted(Dir(quote.Text));
								handler.AppendLiteral(">");
								handler.AppendFormatted(Inline(quote.Spans));
								handler.AppendLiteral("</blockquote>\n");
								stringBuilder3.Append(ref handler);
							}
							else
							{
								StringBuilder stringBuilder = html;
								StringBuilder stringBuilder4 = stringBuilder;
								StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder);
								handler.AppendLiteral("<pre><code>");
								handler.AppendFormatted(Escape(codeBlock.Code));
								handler.AppendLiteral("</code></pre>\n");
								stringBuilder4.Append(ref handler);
							}
						}
						else
						{
							string value = pictureSource?.Invoke(picture) ?? ("data:" + picture.Mime + ";base64," + Convert.ToBase64String(picture.Data));
							StringBuilder stringBuilder = html;
							StringBuilder stringBuilder5 = stringBuilder;
							StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(27, 2, stringBuilder);
							handler.AppendLiteral("<p><img src=\"");
							handler.AppendFormatted(value);
							handler.AppendLiteral("\" alt=\"");
							handler.AppendFormatted(Escape(picture.Alt));
							handler.AppendLiteral("\"></p>\n");
							stringBuilder5.Append(ref handler);
						}
						continue;
					}
					html.Append("<table>\n");
					for (int i = 0; i < table.Rows.Count; i++)
					{
						string tag = ((i == 0 && table.HeaderRow) ? "th" : "td");
						html.Append("<tr>").Append(string.Concat(table.Rows[i].Select((List<Span> cell) => $"<{tag}{Dir(string.Concat(cell.Select((Span s) => s.Text)))}>{Inline(cell)}</{tag}>"))).Append("</tr>\n");
					}
					html.Append("</table>\n");
				}
				else
				{
					string value2 = paragraph.Align switch
					{
						Align.Center => " style=\"text-align:center\"", 
						Align.End => " style=\"text-align:end\"", 
						Align.Justify => " style=\"text-align:justify\"", 
						_ => "", 
					};
					StringBuilder stringBuilder = html;
					StringBuilder stringBuilder6 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
					handler.AppendLiteral("<p");
					handler.AppendFormatted(Dir(paragraph.Text));
					handler.AppendFormatted(value2);
					handler.AppendLiteral(">");
					handler.AppendFormatted(Inline(paragraph.Spans).Replace("\n", "<br>"));
					handler.AppendLiteral("</p>\n");
					stringBuilder6.Append(ref handler);
				}
			}
			else
			{
				int value3 = Math.Clamp(heading.Level, 1, 6);
				StringBuilder stringBuilder = html;
				StringBuilder stringBuilder7 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 4, stringBuilder);
				handler.AppendLiteral("<h");
				handler.AppendFormatted(value3);
				handler.AppendFormatted(Dir(heading.Text));
				handler.AppendLiteral(">");
				handler.AppendFormatted(Inline(heading.Spans));
				handler.AppendLiteral("</h");
				handler.AppendFormatted(value3);
				handler.AppendLiteral(">\n");
				stringBuilder7.Append(ref handler);
			}
		}
		if (flag.HasValue)
		{
			html.Append((flag == true) ? "</ol>\n" : "</ul>\n");
		}
	}

	private static string Dir(string text)
	{
		if (!Bidi.IsRightToLeft(text))
		{
			return "";
		}
		return " dir=\"rtl\"";
	}

	public static string Inline(IEnumerable<Span> spans)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Span span in spans)
		{
			string text = Escape(span.Text);
			if (span.Code)
			{
				text = "<code>" + text + "</code>";
			}
			if (span.Bold)
			{
				text = "<strong>" + text + "</strong>";
			}
			if (span.Italic)
			{
				text = "<em>" + text + "</em>";
			}
			if (span.Underline)
			{
				text = "<u>" + text + "</u>";
			}
			if (span.Link != null)
			{
				text = $"<a href=\"{Escape(span.Link)}\">{text}</a>";
			}
			stringBuilder.Append(text);
		}
		return stringBuilder.ToString();
	}

	public static string Escape(string text)
	{
		return WebUtility.HtmlEncode(text);
	}

	public static string Markdown(Doc doc)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (Block block in doc.Blocks)
		{
			if (!(block is ListItem))
			{
				num = 0;
			}
			if (!(block is Heading heading))
			{
				if (!(block is Paragraph paragraph))
				{
					if (!(block is ListItem listItem))
					{
						if (!(block is Table table))
						{
							if (!(block is Picture picture))
							{
								if (!(block is CodeBlock codeBlock))
								{
									if (!(block is Quote quote))
									{
										if (block is PageBreak)
										{
											stringBuilder.Append("\n---\n\n");
										}
									}
									else
									{
										stringBuilder.Append("> ").Append(MdInline(quote.Spans)).Append("\n\n");
									}
								}
								else
								{
									stringBuilder.Append("```\n").Append(codeBlock.Code.TrimEnd()).Append("\n```\n\n");
								}
							}
							else
							{
								StringBuilder stringBuilder2 = stringBuilder;
								StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 3, stringBuilder2);
								handler.AppendLiteral("![");
								handler.AppendFormatted(picture.Alt);
								handler.AppendLiteral("](data:");
								handler.AppendFormatted(picture.Mime);
								handler.AppendLiteral(";base64,");
								handler.AppendFormatted(Convert.ToBase64String(picture.Data));
								handler.AppendLiteral(")\n\n");
								stringBuilder2.Append(ref handler);
							}
						}
						else
						{
							for (int i = 0; i < table.Rows.Count; i++)
							{
								stringBuilder.Append("| ").Append(string.Join(" | ", table.Rows[i].Select((List<Span> cell) => MdInline(cell).Replace("|", "\\|")))).Append(" |\n");
								if (i == 0)
								{
									stringBuilder.Append('|').Append(string.Concat(table.Rows[0].Select((List<Span> _) => " --- |"))).Append('\n');
								}
							}
							stringBuilder.Append('\n');
						}
					}
					else
					{
						stringBuilder.Append(new string(' ', listItem.Depth * 2)).Append(listItem.Ordered ? $"{++num}. " : "- ").Append(MdInline(listItem.Spans))
							.Append('\n');
					}
				}
				else
				{
					stringBuilder.Append(MdInline(paragraph.Spans).Replace("\n", "  \n")).Append("\n\n");
				}
			}
			else
			{
				stringBuilder.Append(new string('#', Math.Clamp(heading.Level, 1, 6))).Append(' ').Append(MdInline(heading.Spans))
					.Append("\n\n");
			}
			_ = block is ListItem;
		}
		return stringBuilder.ToString().TrimEnd() + "\n";
	}

	private static string MdInline(IEnumerable<Span> spans)
	{
		return string.Concat(spans.Select(delegate(Span span)
		{
			if (span.Text.Length == 0)
			{
				return "";
			}
			string text = (span.Code ? ("`" + span.Text + "`") : span.Text.Replace("*", "\\*").Replace("_", "\\_"));
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				return text;
			}
			string text3 = text.Substring(0, text.IndexOf(text2[0]));
			string text4 = text2;
			string text5 = text.Substring(text.LastIndexOf(text4[text4.Length - 1]) + 1);
			if (span.Bold)
			{
				text2 = "**" + text2 + "**";
			}
			if (span.Italic)
			{
				text2 = "*" + text2 + "*";
			}
			if (span.Link != null)
			{
				text2 = $"[{text2}]({span.Link})";
			}
			return text3 + text2 + text5;
		}));
	}
}
