using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ShortTech.Core.Documents;

public static class HtmlReader
{
	private sealed class Walker(Doc doc, Func<string, (byte[] Data, string Ext)?>? resolveImage)
	{
		private readonly List<Span> _pending = new List<Span>();

		public void Blocks(HtmlNode node)
		{
			foreach (HtmlNode childNode in node.ChildNodes)
			{
				Block(childNode);
			}
			Flush();
		}

		private void Block(HtmlNode node)
		{
			if (node.NodeType == HtmlNodeType.Text)
			{
				Inline(node, new Span(""));
			}
			else
			{
				if (node.NodeType != HtmlNodeType.Element)
				{
					return;
				}
				string text = node.Name.ToLowerInvariant();
				switch (text)
				{
				case "script":
					break;
				case "style":
					break;
				case "head":
					break;
				case "noscript":
					break;
				case "template":
					break;
				case "h1":
				case "h2":
				case "h3":
				case "h4":
				case "h5":
				case "h6":
					Flush();
					doc.Blocks.Add(new Heading(text[1] - 48, Spans(node)));
					break;
				case "header":
				case "footer":
				case "figure":
				case "center":
				case "aside":
				case "main":
				case "nav":
				case "div":
				case "article":
				case "section":
				case "figcaption":
				case "p":
				{
					if (node.ChildNodes.Any((HtmlNode child) => IsBlock(child.Name)))
					{
						Flush();
						foreach (HtmlNode childNode in node.ChildNodes)
						{
							Block(childNode);
						}
						Flush();
						break;
					}
					Flush();
					List<Span> list2 = Spans(node);
					if (list2.Any((Span s) => s.Text.Trim().Length > 0))
					{
						doc.Blocks.Add(new Paragraph(list2, AlignOf(node)));
					}
					{
						foreach (HtmlNode item in node.Descendants("img"))
						{
							Image(item);
						}
						break;
					}
				}
				case "ul":
				case "ol":
					Flush();
					List(node, text == "ol", 0);
					break;
				case "table":
				{
					Flush();
					List<List<List<Span>>> list = (from tr in node.Descendants("tr")
						select tr.ChildNodes.Where(delegate(HtmlNode c)
						{
							string name = c.Name;
							return (name == "td" || name == "th") ? true : false;
						}).Select(Spans).ToList() into r
						where r.Count > 0
						select r).ToList();
					if (list.Count > 0)
					{
						doc.Blocks.Add(new Table(list, node.Descendants("th").Any()));
					}
					break;
				}
				case "pre":
					Flush();
					doc.Blocks.Add(new CodeBlock(WebUtility.HtmlDecode(node.InnerText)));
					break;
				case "blockquote":
					Flush();
					doc.Blocks.Add(new Quote(Spans(node)));
					break;
				case "img":
					Flush();
					Image(node);
					break;
				case "hr":
					Flush();
					if (node.GetAttributeValue("class", "").Contains("break") || node.GetAttributeValue("style", "").Contains("page-break"))
					{
						doc.Blocks.Add(new PageBreak());
					}
					break;
				case "br":
					_pending.Add(new Span("\n"));
					break;
				default:
					if (node.GetAttributeValue("style", "").Contains("page-break-after") || node.GetAttributeValue("class", "") == "break")
					{
						Flush();
						doc.Blocks.Add(new PageBreak());
					}
					else
					{
						Inline(node, new Span(""));
					}
					break;
				}
			}
		}

		private void List(HtmlNode list, bool ordered, int depth)
		{
			foreach (HtmlNode item in list.ChildNodes.Where((HtmlNode c) => c.Name == "li"))
			{
				List<Span> list2 = new List<Span>();
				foreach (HtmlNode item2 in item.ChildNodes.Where(delegate(HtmlNode c)
				{
					string name = c.Name;
					bool flag = ((name == "ul" || name == "ol") ? true : false);
					return !flag;
				}))
				{
					Collect(item2, new Span(""), list2);
				}
				doc.Blocks.Add(new ListItem(Clean(list2), ordered, depth));
				foreach (HtmlNode item3 in item.ChildNodes.Where(delegate(HtmlNode c)
				{
					string name = c.Name;
					return (name == "ul" || name == "ol") ? true : false;
				}))
				{
					List(item3, item3.Name == "ol", depth + 1);
				}
			}
		}

		private void Image(HtmlNode img)
		{
			string attributeValue = img.GetAttributeValue("src", "");
			if (attributeValue.Length == 0)
			{
				return;
			}
			(byte[], string)? tuple = null;
			if (attributeValue.StartsWith("data:"))
			{
				int num = attributeValue.IndexOf(',');
				string text = attributeValue.Substring(5, Math.Max(5, num) - 5);
				if (num > 0 && text.Contains("base64"))
				{
					try
					{
						tuple = (Convert.FromBase64String(attributeValue.Substring(num + 1)), text.Contains("png") ? "png" : (text.Contains("gif") ? "gif" : (text.Contains("webp") ? "webp" : "jpg")));
					}
					catch (FormatException)
					{
					}
				}
			}
			else
			{
				tuple = resolveImage?.Invoke(WebUtility.UrlDecode(attributeValue));
			}
			if (tuple.HasValue)
			{
				(byte[], string) valueOrDefault = tuple.GetValueOrDefault();
				doc.Blocks.Add(new Picture(valueOrDefault.Item1, valueOrDefault.Item2, 0, 0, img.GetAttributeValue("alt", "")));
			}
		}

		private void Inline(HtmlNode node, Span style)
		{
			Collect(node, style, _pending);
		}

		private List<Span> Spans(HtmlNode node)
		{
			List<Span> list = new List<Span>();
			foreach (HtmlNode childNode in node.ChildNodes)
			{
				Collect(childNode, new Span(""), list);
			}
			return Clean(list);
		}

		private static void Collect(HtmlNode node, Span style, List<Span> into)
		{
			if (node.NodeType == HtmlNodeType.Text)
			{
				string input = WebUtility.HtmlDecode(node.InnerText);
				input = Regex.Replace(input, "\\s+", " ");
				if (input.Length > 0)
				{
					into.Add(style with
					{
						Text = input
					});
				}
				return;
			}
			if (node.NodeType != HtmlNodeType.Element)
			{
				return;
			}
			string text = node.Name.ToLowerInvariant();
			bool flag;
			switch (text)
			{
			case "script":
			case "style":
			case "img":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				return;
			}
			if (text == "br")
			{
				into.Add(new Span("\n"));
				return;
			}
			string attributeValue = node.GetAttributeValue("style", "");
			string? link = style.Link;
			if (text == "a")
			{
				string attributeValue2 = node.GetAttributeValue("href", "");
				if (attributeValue2 != null && attributeValue2.Length > 0 && !attributeValue2.StartsWith('#'))
				{
					link = attributeValue2;
				}
			}
			Span style2 = style with
			{
				Bold = style.Bold || text == "b" || text == "strong" || attributeValue.Contains("font-weight:bold") || attributeValue.Contains("font-weight: bold") || attributeValue.Contains("font-weight:700"),
				Italic = style.Italic || text == "i" || text == "em" || attributeValue.Contains("italic"),
				Underline = style.Underline || text == "u" || attributeValue.Contains("underline"),
				Code = style.Code || text == "code" || text == "kbd" || text == "tt",
				Link = link
			};
			foreach (HtmlNode childNode in node.ChildNodes)
			{
				Collect(childNode, style2, into);
			}
		}

		private static List<Span> Clean(List<Span> spans)
		{
			List<Span> list = new List<Span>();
			foreach (Span span3 in spans)
			{
				if (list.Count > 0)
				{
					if (list[list.Count - 1]with
					{
						Text = ""
					} == span3 with
					{
						Text = ""
					})
					{
						int index = list.Count - 1;
						list[index] = list[index] with
						{
							Text = list[index].Text + span3.Text
						};
						continue;
					}
				}
				list.Add(span3);
			}
			if (list.Count > 0)
			{
				list[0] = list[0]with
				{
					Text = list[0].Text.TrimStart()
				};
			}
			if (list.Count > 0)
			{
				int index2 = list.Count - 1;
				list[index2] = list[index2] with
				{
					Text = list[index2].Text.TrimEnd()
				};
			}
			return list.Where((Span s) => s.Text.Length > 0).ToList();
		}

		private void Flush()
		{
			List<Span> list = Clean(_pending.ToList());
			_pending.Clear();
			if (list.Any((Span s) => s.Text.Trim().Length > 0))
			{
				doc.Blocks.Add(new Paragraph(list));
			}
		}

		private static bool IsBlock(string name)
		{
			switch (name)
			{
			case "div":
			case "img":
			case "pre":
			case "h1":
			case "h2":
			case "h3":
			case "h4":
			case "h5":
			case "h6":
			case "ul":
			case "ol":
			case "hr":
			case "article":
			case "section":
			case "header":
			case "figure":
			case "footer":
			case "p":
			case "table":
			case "blockquote":
			case "main":
				return true;
			default:
				return false;
			}
		}

		private static Align AlignOf(HtmlNode node)
		{
			string text = (node.GetAttributeValue("style", "") + " " + node.GetAttributeValue("align", "")).ToLowerInvariant();
			if (text.Contains("center"))
			{
				return Align.Center;
			}
			if (text.Contains("justify"))
			{
				return Align.Justify;
			}
			if (text.Contains("right") || text.Contains("end"))
			{
				return Align.End;
			}
			return Align.Start;
		}
	}

	public static Doc Read(string html, string title = "Untitled", Func<string, (byte[] Data, string Ext)?>? resolveImage = null, Doc? into = null)
	{
		HtmlDocument htmlDocument = new HtmlDocument();
		htmlDocument.LoadHtml(html);
		Doc doc = into ?? new Doc
		{
			Title = title
		};
		string value = htmlDocument.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
		if (into == null && !string.IsNullOrEmpty(value))
		{
			doc.Title = WebUtility.HtmlDecode(value);
		}
		HtmlNode node = htmlDocument.DocumentNode.SelectSingleNode("//body") ?? htmlDocument.DocumentNode;
		new Walker(doc, resolveImage).Blocks(node);
		return doc;
	}
}
