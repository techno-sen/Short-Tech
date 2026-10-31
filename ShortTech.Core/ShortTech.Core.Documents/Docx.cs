using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Pictures;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ImageMagick;

namespace ShortTech.Core.Documents;

public static class Docx
{
	public static Doc Read(string path)
	{
		using WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(path, isEditable: false);
		MainDocumentPart main = wordprocessingDocument.MainDocumentPart ?? throw new ShortTechException(System.IO.Path.GetFileName(path) + " could not be read as a document");
		Doc doc = new Doc();
		string title = wordprocessingDocument.PackageProperties.Title;
		doc.Title = ((title != null && title.Length > 0) ? title : System.IO.Path.GetFileNameWithoutExtension(path));
		Doc doc2 = doc;
		Body body = main.Document?.Body;
		if (body == null)
		{
			return doc2;
		}
		foreach (OpenXmlElement childElement in body.ChildElements)
		{
			if (!(childElement is DocumentFormat.OpenXml.Wordprocessing.Paragraph p))
			{
				if (!(childElement is DocumentFormat.OpenXml.Wordprocessing.Table table))
				{
					continue;
				}
				List<List<List<Span>>> list = (from row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>()
					select (from cell in row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
						select cell.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().SelectMany((DocumentFormat.OpenXml.Wordprocessing.Paragraph para, int i) => ((i <= 0) ? ((IEnumerable<Span>)Array.Empty<Span>()) : ((IEnumerable<Span>)new Span[1]
						{
							new Span("\n")
						})).Concat(Runs(para, main))).ToList()).ToList()).ToList();
				if (list.Count > 0)
				{
					doc2.Blocks.Add(new Table(list));
				}
			}
			else
			{
				ReadParagraph(p, main, doc2);
			}
		}
		return doc2;
	}

	private static void ReadParagraph(DocumentFormat.OpenXml.Wordprocessing.Paragraph p, MainDocumentPart main, Doc doc)
	{
		foreach (Drawing item in p.Descendants<Drawing>())
		{
			string text = item.Descendants<Blip>().FirstOrDefault()?.Embed?.Value;
			if (text == null || !(main.GetPartById(text) is ImagePart imagePart))
			{
				continue;
			}
			using Stream stream = imagePart.GetStream();
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			string extension = (imagePart.ContentType.Contains("png") ? "png" : (imagePart.ContentType.Contains("gif") ? "gif" : "jpg"));
			doc.Blocks.Add(new Picture(memoryStream.ToArray(), extension));
		}
		if (p.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>().Any((DocumentFormat.OpenXml.Wordprocessing.Break b) => b.Type?.Value == BreakValues.Page))
		{
			doc.Blocks.Add(new PageBreak());
		}
		List<Span> list = Runs(p, main);
		if (list.Count == 0 || list.All((Span s) => s.Text.Trim().Length == 0))
		{
			return;
		}
		string text2 = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
		int? num = p.ParagraphProperties?.OutlineLevel?.Val?.Value;
		if (text2.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) && int.TryParse(text2.Substring(7), out var result))
		{
			doc.Blocks.Add(new Heading(result, list));
			return;
		}
		if (text2.Equals("Title", StringComparison.OrdinalIgnoreCase))
		{
			doc.Blocks.Add(new Heading(1, list));
			return;
		}
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault < 6)
			{
				doc.Blocks.Add(new Heading(valueOrDefault + 1, list));
				return;
			}
		}
		NumberingProperties numberingProperties = p.ParagraphProperties?.NumberingProperties;
		if (numberingProperties != null)
		{
			int valueOrDefault2 = (numberingProperties.NumberingLevelReference?.Val?.Value).GetValueOrDefault();
			bool ordered = IsOrdered(main, (numberingProperties.NumberingId?.Val?.Value).GetValueOrDefault(), valueOrDefault2);
			doc.Blocks.Add(new ListItem(list, ordered, valueOrDefault2));
		}
		else
		{
			JustificationValues? justificationValues = p.ParagraphProperties?.Justification?.Val?.Value;
			Align align = ((justificationValues == JustificationValues.Center) ? Align.Center : ((justificationValues == JustificationValues.Both) ? Align.Justify : ((justificationValues == JustificationValues.Right || justificationValues == JustificationValues.End) ? Align.End : Align.Start)));
			doc.Blocks.Add(new Paragraph(list, align));
		}
	}

	private static bool IsOrdered(MainDocumentPart main, int numberingId, int level)
	{
		Numbering? obj = main.NumberingDefinitionsPart?.Numbering;
		int? abstractId = (obj?.Elements<NumberingInstance>().FirstOrDefault(delegate(NumberingInstance n)
		{
			Int32Value? numberID = n.NumberID;
			return (object)numberID != null && numberID.Value == numberingId;
		}))?.AbstractNumId?.Val?.Value;
		NumberFormatValues? numberFormatValues = (obj?.Elements<AbstractNum>().FirstOrDefault(delegate(AbstractNum a)
		{
			Int32Value? abstractNumberId = a.AbstractNumberId;
			return ((object)abstractNumberId == null) ? (!abstractId.HasValue) : (abstractNumberId.Value == abstractId);
		}))?.Elements<Level>().FirstOrDefault(delegate(Level l)
		{
			Int32Value? levelIndex = l.LevelIndex;
			return (object)levelIndex != null && levelIndex.Value == level;
		})?.NumberingFormat?.Val?.Value;
		if (numberFormatValues.HasValue)
		{
			return numberFormatValues != NumberFormatValues.Bullet;
		}
		return false;
	}

	private static List<Span> Runs(OpenXmlElement container, MainDocumentPart main)
	{
		List<Span> list = new List<Span>();
		foreach (DocumentFormat.OpenXml.Wordprocessing.Run item in from r in container.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>()
			where r.Ancestors<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().FirstOrDefault() == container
			select r)
		{
			string text = string.Concat(item.ChildElements.Select(delegate(OpenXmlElement child)
			{
				if (child is DocumentFormat.OpenXml.Wordprocessing.Text text5)
				{
					return text5.Text;
				}
				if (child is TabChar)
				{
					return "\t";
				}
				return (child is DocumentFormat.OpenXml.Wordprocessing.Break obj && (obj.Type == null || obj.Type.Value == BreakValues.TextWrapping)) ? "\n" : "";
			}));
			if (text.Length == 0)
			{
				continue;
			}
			DocumentFormat.OpenXml.Wordprocessing.RunProperties runProperties = item.RunProperties;
			string text2 = null;
			if (item.Parent is DocumentFormat.OpenXml.Wordprocessing.Hyperlink hyperlink)
			{
				string id = hyperlink.Id?.Value;
				if (id != null)
				{
					text2 = main.HyperlinkRelationships.FirstOrDefault((HyperlinkRelationship r) => r.Id == id)?.Uri.ToString();
				}
			}
			List<Span> list2 = list;
			string text3 = text;
			bool bold = runProperties?.Bold != null && (runProperties.Bold.Val?.Value ?? true);
			bool italic = runProperties?.Italic != null && (runProperties.Italic.Val?.Value ?? true);
			UnderlineValues? underlineValues = runProperties?.Underline?.Val?.Value;
			int num;
			if (underlineValues.HasValue)
			{
				UnderlineValues valueOrDefault = underlineValues.GetValueOrDefault();
				num = ((valueOrDefault != UnderlineValues.None) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool underline = (byte)num != 0;
			string link = text2;
			string text4 = runProperties?.RunFonts?.Ascii?.Value;
			bool code = ((text4 == "Consolas" || text4 == "Courier New") ? true : false);
			list2.Add(new Span(text3, bold, italic, underline, link, code));
		}
		return list;
	}

	public static void Write(Doc doc, string path)
	{
		using WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
		wordprocessingDocument.PackageProperties.Title = doc.Title;
		wordprocessingDocument.PackageProperties.Creator = "Short Tech";
		MainDocumentPart mainDocumentPart = wordprocessingDocument.AddMainDocumentPart();
		AddStyles(mainDocumentPart);
		(int, int) tuple = AddNumbering(mainDocumentPart);
		Body body = new Body();
		uint num = 1u;
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
								if (!(block is Table t))
								{
									if (!(block is Picture pic))
									{
										if (block is PageBreak)
										{
											body.Append(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Break
											{
												Type = BreakValues.Page
											})));
										}
									}
									else
									{
										body.Append(PictureParagraph(pic, mainDocumentPart, num++));
									}
								}
								else
								{
									body.Append(TableOf(t, mainDocumentPart));
								}
							}
							else
							{
								string[] array = codeBlock.Code.TrimEnd().Split('\n');
								foreach (string text in array)
								{
									OpenXmlElement[] array2 = new OpenXmlElement[1];
									int num2 = 1;
									List<Span> list = new List<Span>(num2);
									CollectionsMarshal.SetCount(list, num2);
									CollectionsMarshal.AsSpan(list)[0] = new Span(text.TrimEnd('\r'), Bold: false, Italic: false, Underline: false, null, Code: true);
									array2[0] = Para(list, mainDocumentPart, "Code");
									body.Append(array2);
								}
							}
						}
						else
						{
							DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph2 = Para(listItem.Spans, mainDocumentPart, "ListParagraph", Align.Start, Bidi.IsRightToLeft(listItem.Text));
							DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties? paragraphProperties = paragraph2.ParagraphProperties;
							OpenXmlElement[] obj = new OpenXmlElement[2]
							{
								new NumberingLevelReference
								{
									Val = Math.Min(listItem.Depth, 8)
								},
								null
							};
							NumberingId numberingId = new NumberingId();
							int num3;
							if (!listItem.Ordered)
							{
								(num3, _) = tuple;
							}
							else
							{
								num3 = tuple.Item2;
							}
							numberingId.Val = num3;
							obj[1] = numberingId;
							paragraphProperties.NumberingProperties = new NumberingProperties(obj);
							body.Append(paragraph2);
						}
					}
					else
					{
						body.Append(Para(quote.Spans, mainDocumentPart, "Quote", Align.Start, Bidi.IsRightToLeft(quote.Text)));
					}
				}
				else
				{
					body.Append(Para(paragraph.Spans, mainDocumentPart, null, paragraph.Align, Bidi.IsRightToLeft(paragraph.Text)));
				}
			}
			else
			{
				body.Append(Para(heading.Spans, mainDocumentPart, $"Heading{Math.Clamp(heading.Level, 1, 6)}", Align.Start, Bidi.IsRightToLeft(heading.Text)));
			}
		}
		body.Append(new SectionProperties(new PageSize
		{
			Width = 12240u,
			Height = 15840u
		}, new PageMargin
		{
			Top = 1440,
			Bottom = 1440,
			Left = 1440u,
			Right = 1440u,
			Header = 720u,
			Footer = 720u
		}));
		mainDocumentPart.Document = new Document(body);
		mainDocumentPart.Document.Save();
	}

	private static DocumentFormat.OpenXml.Wordprocessing.Paragraph Para(List<Span> spans, MainDocumentPart main, string? style, Align align = Align.Start, bool rtl = false)
	{
		DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties paragraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
		if (style != null)
		{
			paragraphProperties.ParagraphStyleId = new ParagraphStyleId
			{
				Val = style
			};
		}
		if (rtl)
		{
			paragraphProperties.BiDi = new BiDi();
		}
		if (align != Align.Start)
		{
			DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties paragraphProperties2 = paragraphProperties;
			Justification justification = new Justification();
			Justification justification2 = justification;
			justification2.Val = align switch
			{
				Align.Center => JustificationValues.Center, 
				Align.Justify => JustificationValues.Both, 
				_ => rtl ? JustificationValues.Left : JustificationValues.Right, 
			};
			paragraphProperties2.Justification = justification;
		}
		DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(paragraphProperties);
		foreach (Span span in spans)
		{
			string[] array = span.Text.Split('\n');
			DocumentFormat.OpenXml.Wordprocessing.Run run = new DocumentFormat.OpenXml.Wordprocessing.Run();
			DocumentFormat.OpenXml.Wordprocessing.RunProperties runProperties = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
			if (span.Bold)
			{
				runProperties.Append(new Bold(), new BoldComplexScript());
			}
			if (span.Italic)
			{
				runProperties.Append(new Italic(), new ItalicComplexScript());
			}
			if (span.Underline)
			{
				runProperties.Append(new DocumentFormat.OpenXml.Wordprocessing.Underline
				{
					Val = UnderlineValues.Single
				});
			}
			if (span.Code)
			{
				runProperties.Append(new RunFonts
				{
					Ascii = "Consolas",
					HighAnsi = "Consolas"
				});
			}
			if (rtl || Bidi.IsRightToLeft(span.Text))
			{
				runProperties.Append(new RightToLeftText());
			}
			if (span.Link != null)
			{
				runProperties.Append(new RunStyle
				{
					Val = "Hyperlink"
				});
			}
			if (runProperties.HasChildren)
			{
				run.Append(runProperties);
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0)
				{
					run.Append(new DocumentFormat.OpenXml.Wordprocessing.Break());
				}
				run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text(array[i])
				{
					Space = SpaceProcessingModeValues.Preserve
				});
			}
			if (span.Link != null && Uri.TryCreate(span.Link, UriKind.Absolute, out Uri result))
			{
				HyperlinkRelationship hyperlinkRelationship = main.AddHyperlinkRelationship(result, isExternal: true);
				paragraph.Append(new DocumentFormat.OpenXml.Wordprocessing.Hyperlink(run)
				{
					Id = hyperlinkRelationship.Id
				});
			}
			else
			{
				paragraph.Append(run);
			}
		}
		return paragraph;
	}

	private static DocumentFormat.OpenXml.Wordprocessing.Table TableOf(Table t, MainDocumentPart main)
	{
		DocumentFormat.OpenXml.Wordprocessing.Table table = new DocumentFormat.OpenXml.Wordprocessing.Table(new DocumentFormat.OpenXml.Wordprocessing.TableProperties(new DocumentFormat.OpenXml.Wordprocessing.TableStyle
		{
			Val = "TableGrid"
		}, new TableWidth
		{
			Type = TableWidthUnitValues.Pct,
			Width = "5000"
		}, new TableBorders(new DocumentFormat.OpenXml.Wordprocessing.TopBorder
		{
			Val = BorderValues.Single,
			Size = 4u
		}, new DocumentFormat.OpenXml.Wordprocessing.BottomBorder
		{
			Val = BorderValues.Single,
			Size = 4u
		}, new DocumentFormat.OpenXml.Wordprocessing.LeftBorder
		{
			Val = BorderValues.Single,
			Size = 4u
		}, new DocumentFormat.OpenXml.Wordprocessing.RightBorder
		{
			Val = BorderValues.Single,
			Size = 4u
		}, new DocumentFormat.OpenXml.Wordprocessing.InsideHorizontalBorder
		{
			Val = BorderValues.Single,
			Size = 4u
		}, new DocumentFormat.OpenXml.Wordprocessing.InsideVerticalBorder
		{
			Val = BorderValues.Single,
			Size = 4u
		})));
		for (int i = 0; i < t.Rows.Count; i++)
		{
			DocumentFormat.OpenXml.Wordprocessing.TableRow tableRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
			foreach (List<Span> item in t.Rows[i])
			{
				List<Span> spans = ((i == 0 && t.HeaderRow) ? item.Select((Span s) => s with
				{
					Bold = true
				}).ToList() : item);
				tableRow.Append(new DocumentFormat.OpenXml.Wordprocessing.TableCell(Para(spans, main, null, Align.Start, Bidi.IsRightToLeft(string.Concat(item.Select((Span s) => s.Text))))));
			}
			table.Append(tableRow);
		}
		return table;
	}

	private static DocumentFormat.OpenXml.Wordprocessing.Paragraph PictureParagraph(Picture pic, MainDocumentPart main, uint id)
	{
		string extension = pic.Extension;
		PartTypeInfo partTypeInfo = ((extension == "png") ? ImagePartType.Png : ((!(extension == "gif")) ? ImagePartType.Jpeg : ImagePartType.Gif));
		PartTypeInfo partType = partTypeInfo;
		byte[] array = pic.Data;
		bool flag;
		switch (pic.Extension)
		{
		case "webp":
		case "svg":
		case "heic":
		case "avif":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			using MagickImage magickImage = new MagickImage(pic.Data);
			array = magickImage.ToByteArray(MagickFormat.Png);
			partType = ImagePartType.Png;
		}
		ImagePart imagePart = main.AddImagePart(partType);
		using (MemoryStream sourceStream = new MemoryStream(array))
		{
			imagePart.FeedData(sourceStream);
		}
		int pixelWidth = pic.PixelWidth;
		int num = pic.PixelHeight;
		int num2 = pixelWidth;
		if (num2 <= 0 || num <= 0)
		{
			MagickImageInfo magickImageInfo = new MagickImageInfo(array);
			int width = (int)magickImageInfo.Width;
			num = (int)magickImageInfo.Height;
			num2 = width;
		}
		long num3 = (long)num2 * 9525L;
		long num4 = (long)num * 9525L;
		if (num3 > 5943600)
		{
			num4 = num4 * 5943600 / num3;
			num3 = 5943600L;
		}
		string idOfPart = main.GetIdOfPart(imagePart);
		Graphic graphic = new Graphic(new GraphicData(new DocumentFormat.OpenXml.Drawing.Pictures.Picture(new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties
		{
			Id = id,
			Name = $"Picture {id}"
		}, new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()), new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(new Blip
		{
			Embed = idOfPart
		}, new Stretch(new FillRectangle())), new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(new Transform2D(new Offset
		{
			X = 0L,
			Y = 0L
		}, new Extents
		{
			Cx = num3,
			Cy = num4
		}), new PresetGeometry(new AdjustValueList())
		{
			Preset = ShapeTypeValues.Rectangle
		})))
		{
			Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
		});
		Inline inline = new Inline(new Extent
		{
			Cx = num3,
			Cy = num4
		}, new EffectExtent
		{
			LeftEdge = 0L,
			TopEdge = 0L,
			RightEdge = 0L,
			BottomEdge = 0L
		}, new DocProperties
		{
			Id = id,
			Name = $"Picture {id}",
			Description = pic.Alt
		}, new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(new GraphicFrameLocks
		{
			NoChangeAspect = true
		}), graphic)
		{
			DistanceFromTop = 0u,
			DistanceFromBottom = 0u,
			DistanceFromLeft = 0u,
			DistanceFromRight = 0u
		};
		return new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new Drawing(inline)));
	}

	private static void AddStyles(MainDocumentPart main)
	{
		Styles styles = new Styles(new DocDefaults(new RunPropertiesDefault(new RunPropertiesBaseStyle(new RunFonts
		{
			Ascii = "Calibri",
			HighAnsi = "Calibri",
			ComplexScript = "Arial"
		}, new FontSize
		{
			Val = "22"
		}, new FontSizeComplexScript
		{
			Val = "22"
		})), new ParagraphPropertiesDefault(new ParagraphPropertiesBaseStyle(new SpacingBetweenLines
		{
			After = "160",
			Line = "276",
			LineRule = LineSpacingRuleValues.Auto
		}))));
		styles.Append(new Style(new StyleName
		{
			Val = "Normal"
		}, new PrimaryStyle())
		{
			Type = StyleValues.Paragraph,
			StyleId = "Normal",
			Default = true
		});
		int[] array = new int[6] { 40, 32, 28, 26, 24, 22 };
		for (int i = 1; i <= 6; i++)
		{
			styles.Append(new Style(new StyleName
			{
				Val = $"heading {i}"
			}, new BasedOn
			{
				Val = "Normal"
			}, new NextParagraphStyle
			{
				Val = "Normal"
			}, new PrimaryStyle(), new StyleParagraphProperties(new KeepNext(), new SpacingBetweenLines
			{
				Before = "240",
				After = "80"
			}, new OutlineLevel
			{
				Val = i - 1
			}), new StyleRunProperties(new Bold(), new BoldComplexScript(), new FontSize
			{
				Val = array[i - 1].ToString()
			}, new FontSizeComplexScript
			{
				Val = array[i - 1].ToString()
			}, new Color
			{
				Val = "1F1F2E"
			}))
			{
				Type = StyleValues.Paragraph,
				StyleId = $"Heading{i}"
			});
		}
		styles.Append(new Style(new StyleName
		{
			Val = "List Paragraph"
		}, new BasedOn
		{
			Val = "Normal"
		}, new StyleParagraphProperties(new Indentation
		{
			Left = "720"
		}, new ContextualSpacing()))
		{
			Type = StyleValues.Paragraph,
			StyleId = "ListParagraph"
		});
		styles.Append(new Style(new StyleName
		{
			Val = "Quote"
		}, new BasedOn
		{
			Val = "Normal"
		}, new StyleParagraphProperties(new Indentation
		{
			Left = "720"
		}), new StyleRunProperties(new Italic(), new Color
		{
			Val = "555555"
		}))
		{
			Type = StyleValues.Paragraph,
			StyleId = "Quote"
		});
		styles.Append(new Style(new StyleName
		{
			Val = "Code"
		}, new BasedOn
		{
			Val = "Normal"
		}, new StyleParagraphProperties(new SpacingBetweenLines
		{
			After = "0"
		}), new StyleRunProperties(new RunFonts
		{
			Ascii = "Consolas",
			HighAnsi = "Consolas"
		}, new FontSize
		{
			Val = "20"
		}))
		{
			Type = StyleValues.Paragraph,
			StyleId = "Code"
		});
		styles.Append(new Style(new StyleName
		{
			Val = "Hyperlink"
		}, new StyleRunProperties(new Color
		{
			Val = "2F5BD6"
		}, new DocumentFormat.OpenXml.Wordprocessing.Underline
		{
			Val = UnderlineValues.Single
		}))
		{
			Type = StyleValues.Character,
			StyleId = "Hyperlink"
		});
		styles.Append(new Style(new StyleName
		{
			Val = "Table Grid"
		})
		{
			Type = StyleValues.Table,
			StyleId = "TableGrid"
		});
		main.AddNewPart<StyleDefinitionsPart>().Styles = styles;
	}

	private static (int Bullets, int Ordered) AddNumbering(MainDocumentPart main)
	{
		Numbering numbering = new Numbering(Definition(0, ordered: false), Definition(1, ordered: true), new NumberingInstance(new AbstractNumId
		{
			Val = 0
		})
		{
			NumberID = 1
		}, new NumberingInstance(new AbstractNumId
		{
			Val = 1
		})
		{
			NumberID = 2
		});
		main.AddNewPart<NumberingDefinitionsPart>().Numbering = numbering;
		return (Bullets: 1, Ordered: 2);
		static AbstractNum Definition(int id, bool ordered)
		{
			AbstractNum abstractNum = new AbstractNum
			{
				AbstractNumberId = id
			};
			for (int i = 0; i < 9; i++)
			{
				abstractNum.Append(new Level(new StartNumberingValue
				{
					Val = 1
				}, new NumberingFormat
				{
					Val = (ordered ? NumberFormatValues.Decimal : NumberFormatValues.Bullet)
				}, new LevelText
				{
					Val = (ordered ? $"%{i + 1}." : ((i % 2 == 0) ? "•" : "◦"))
				}, new LevelJustification
				{
					Val = LevelJustificationValues.Left
				}, new PreviousParagraphProperties(new Indentation
				{
					Left = (720 + 360 * i).ToString(),
					Hanging = "360"
				}))
				{
					LevelIndex = i
				});
			}
			return abstractNum;
		}
	}
}
