using System;
using System.Collections.Generic;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace ShortTech.Core.Documents;

internal static class Pptx
{
	public static void Write(List<(byte[] Png, long Cx, long Cy)> slides, string path)
	{
		using PresentationDocument presentationDocument = PresentationDocument.Create(path, PresentationDocumentType.Presentation);
		PresentationPart presentationPart = presentationDocument.AddPresentationPart();
		long num;
		long num2;
		if (slides.Count <= 0)
		{
			num = 12192000L;
			num2 = 6858000L;
		}
		else
		{
			long item = slides[0].Cx;
			long item2 = slides[0].Cy;
			num2 = item2;
			num = item;
		}
		SlideMasterPart slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>("rId1");
		SlideLayoutPart slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>("rId1");
		slideLayoutPart.SlideLayout = new SlideLayout(new CommonSlideData(EmptyTree()), new ColorMapOverride(new MasterColorMapping()))
		{
			Type = SlideLayoutValues.Blank
		};
		slideMasterPart.AddNewPart<ThemePart>("rId2").Theme = Theme();
		slideMasterPart.SlideMaster = new SlideMaster(new CommonSlideData(EmptyTree()), new DocumentFormat.OpenXml.Presentation.ColorMap
		{
			Background1 = ColorSchemeIndexValues.Light1,
			Text1 = ColorSchemeIndexValues.Dark1,
			Background2 = ColorSchemeIndexValues.Light2,
			Text2 = ColorSchemeIndexValues.Dark2,
			Accent1 = ColorSchemeIndexValues.Accent1,
			Accent2 = ColorSchemeIndexValues.Accent2,
			Accent3 = ColorSchemeIndexValues.Accent3,
			Accent4 = ColorSchemeIndexValues.Accent4,
			Accent5 = ColorSchemeIndexValues.Accent5,
			Accent6 = ColorSchemeIndexValues.Accent6,
			Hyperlink = ColorSchemeIndexValues.Hyperlink,
			FollowedHyperlink = ColorSchemeIndexValues.FollowedHyperlink
		}, new SlideLayoutIdList(new SlideLayoutId
		{
			Id = 2147483649u,
			RelationshipId = "rId1"
		}));
		SlideIdList slideIdList = new SlideIdList();
		uint num3 = 256u;
		foreach (var slide in slides)
		{
			byte[] item3 = slide.Png;
			long item4 = slide.Cx;
			long item5 = slide.Cy;
			SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
			slidePart.AddPart(slideLayoutPart);
			ImagePart imagePart = slidePart.AddImagePart(ImagePartType.Png);
			using (MemoryStream sourceStream = new MemoryStream(item3))
			{
				imagePart.FeedData(sourceStream);
			}
			string idOfPart = slidePart.GetIdOfPart(imagePart);
			ShapeTree shapeTree = EmptyTree();
			shapeTree.Append(new DocumentFormat.OpenXml.Presentation.Picture(new DocumentFormat.OpenXml.Presentation.NonVisualPictureProperties(new DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties
			{
				Id = 2u,
				Name = "Page"
			}, new DocumentFormat.OpenXml.Presentation.NonVisualPictureDrawingProperties(new PictureLocks
			{
				NoChangeAspect = true
			}), new ApplicationNonVisualDrawingProperties()), new DocumentFormat.OpenXml.Presentation.BlipFill(new Blip
			{
				Embed = idOfPart
			}, new Stretch(new FillRectangle())), new DocumentFormat.OpenXml.Presentation.ShapeProperties(new Transform2D(new Offset
			{
				X = 0L,
				Y = 0L
			}, new Extents
			{
				Cx = num,
				Cy = num * item5 / Math.Max(1L, item4)
			}), new PresetGeometry(new AdjustValueList())
			{
				Preset = ShapeTypeValues.Rectangle
			})));
			slidePart.Slide = new Slide(new CommonSlideData(shapeTree));
			slideIdList.Append(new SlideId
			{
				Id = num3++,
				RelationshipId = presentationPart.GetIdOfPart(slidePart)
			});
		}
		presentationPart.Presentation = new Presentation(new SlideMasterIdList(new SlideMasterId
		{
			Id = 2147483648u,
			RelationshipId = "rId1"
		}), slideIdList, new SlideSize
		{
			Cx = (int)num,
			Cy = (int)num2
		}, new NotesSize
		{
			Cx = 6858000L,
			Cy = 9144000L
		});
	}

	private static ShapeTree EmptyTree()
	{
		return new ShapeTree(new DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeProperties(new DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties
		{
			Id = 1u,
			Name = ""
		}, new DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeDrawingProperties(), new ApplicationNonVisualDrawingProperties()), new GroupShapeProperties(new TransformGroup()));
	}

	private static Theme Theme()
	{
		ColorScheme colorScheme = new ColorScheme(new Dark1Color(new RgbColorModelHex
		{
			Val = "000000"
		}), new Light1Color(new RgbColorModelHex
		{
			Val = "FFFFFF"
		}), new Dark2Color(new RgbColorModelHex
		{
			Val = "1F1F1F"
		}), new Light2Color(new RgbColorModelHex
		{
			Val = "EEEEEE"
		}), new Accent1Color(new RgbColorModelHex
		{
			Val = "5B47E6"
		}), new Accent2Color(new RgbColorModelHex
		{
			Val = "2F7DF6"
		}), new Accent3Color(new RgbColorModelHex
		{
			Val = "33B85A"
		}), new Accent4Color(new RgbColorModelHex
		{
			Val = "FFCC1A"
		}), new Accent5Color(new RgbColorModelHex
		{
			Val = "8E5CD9"
		}), new Accent6Color(new RgbColorModelHex
		{
			Val = "E8433A"
		}), new Hyperlink(new RgbColorModelHex
		{
			Val = "2F7DF6"
		}), new FollowedHyperlinkColor(new RgbColorModelHex
		{
			Val = "8E5CD9"
		}))
		{
			Name = "Short Tech"
		};
		FontScheme fontScheme = new FontScheme(new MajorFont(new LatinFont
		{
			Typeface = "Segoe UI"
		}, new EastAsianFont
		{
			Typeface = ""
		}, new ComplexScriptFont
		{
			Typeface = "Arial"
		}), new MinorFont(new LatinFont
		{
			Typeface = "Segoe UI"
		}, new EastAsianFont
		{
			Typeface = ""
		}, new ComplexScriptFont
		{
			Typeface = "Arial"
		}))
		{
			Name = "Short Tech"
		};
		FormatScheme formatScheme = new FormatScheme(new FillStyleList(Fill("FFFFFF"), Fill("FFFFFF"), Fill("FFFFFF")), new LineStyleList(new Outline(Fill("000000"))
		{
			Width = 9525
		}, new Outline(Fill("000000"))
		{
			Width = 25400
		}, new Outline(Fill("000000"))
		{
			Width = 38100
		}), new EffectStyleList(new EffectStyle(new EffectList()), new EffectStyle(new EffectList()), new EffectStyle(new EffectList())), new BackgroundFillStyleList(Fill("FFFFFF"), Fill("FFFFFF"), Fill("FFFFFF")))
		{
			Name = "Short Tech"
		};
		return new Theme(new ThemeElements(colorScheme, fontScheme, formatScheme))
		{
			Name = "Short Tech"
		};
		static SolidFill Fill(string hex)
		{
			return new SolidFill(new RgbColorModelHex
			{
				Val = hex
			});
		}
	}
}
