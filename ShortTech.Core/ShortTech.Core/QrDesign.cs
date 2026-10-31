using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShortTech.Core;

public sealed record QrDesign
{
	public enum ModuleShape
	{
		Square,
		Rounded,
		Dots,
		Diamond,
		Fluid,
		Bars
	}

	public enum EyeShape
	{
		Square,
		Rounded,
		Circle,
		Leaf
	}

	public enum Plate
	{
		None,
		Square,
		Rounded,
		Circle
	}

	public ModuleShape Module { get; init; }

	public EyeShape Eye { get; init; }

	public Color Foreground { get; init; } = Colors.Black;

	public Color? GradientEnd { get; init; }

	public Color EyeColor { get; init; } = Colors.Black;

	public Color? Background { get; init; } = Colors.White;

	public int Margin { get; init; } = 4;

	public BitmapSource? Logo { get; init; }

	public double LogoScale { get; init; } = 0.22;

	public Plate LogoPlate { get; init; } = Plate.Rounded;

	public Color PlateColor => Background ?? Colors.White;

	public static string Title(ModuleShape shape)
	{
		return shape switch
		{
			ModuleShape.Square => "Squares", 
			ModuleShape.Rounded => "Rounded", 
			ModuleShape.Dots => "Dots", 
			ModuleShape.Diamond => "Diamonds", 
			ModuleShape.Fluid => "Fluid", 
			_ => "Bars", 
		};
	}

	public static string Title(EyeShape shape)
	{
		return shape switch
		{
			EyeShape.Square => "Square", 
			EyeShape.Rounded => "Rounded", 
			EyeShape.Circle => "Circle", 
			_ => "Leaf", 
		};
	}

	public static string Title(Plate plate)
	{
		return plate switch
		{
			Plate.None => "None", 
			Plate.Square => "Square", 
			Plate.Rounded => "Rounded", 
			_ => "Circle", 
		};
	}

	public (Rect Logo, Rect Clear)? LogoBox(QrMatrix matrix)
	{
		if (Logo == null)
		{
			return null;
		}
		double num = Math.Round((double)matrix.Size * LogoScale);
		double num2 = ((double)matrix.Size - num) / 2.0;
		Rect item2;
		Rect item = (item2 = new Rect(num2, num2, num, num));
		item2.Inflate((LogoPlate == Plate.None) ? 0.4 : 0.9, (LogoPlate == Plate.None) ? 0.4 : 0.9);
		return (item, item2);
	}

	public string ModulePath(QrMatrix matrix)
	{
		PathBuilder pathBuilder = new PathBuilder();
		Rect? clear = LogoBox(matrix)?.Clear;
		for (int i = 0; i < matrix.Size; i++)
		{
			for (int j = 0; j < matrix.Size; j++)
			{
				if (Keep(j, i))
				{
					Rect rect = new Rect(j + Margin, i + Margin, 1.0, 1.0);
					switch (Module)
					{
					case ModuleShape.Square:
						pathBuilder.Rect(Inset(rect, -0.01));
						break;
					case ModuleShape.Rounded:
						pathBuilder.RoundedRect(Inset(rect, 0.06), 0.28, 0.28, 0.28, 0.28);
						break;
					case ModuleShape.Dots:
						pathBuilder.Ellipse(Inset(rect, 0.07));
						break;
					case ModuleShape.Diamond:
						pathBuilder.Polygon(new global::_003C_003Ez__ReadOnlyArray<(double, double)>(new(double, double)[4]
						{
							(rect.X + 0.5, rect.Y + 0.02),
							(rect.Right - 0.02, rect.Y + 0.5),
							(rect.X + 0.5, rect.Bottom - 0.02),
							(rect.X + 0.02, rect.Y + 0.5)
						}));
						break;
					case ModuleShape.Fluid:
					{
						bool flag3 = Keep(j, i - 1);
						bool flag4 = Keep(j, i + 1);
						bool flag5 = Keep(j - 1, i);
						bool flag6 = Keep(j + 1, i);
						pathBuilder.RoundedRect(Inset(rect, -0.005), (!flag3 && !flag5) ? 0.5 : 0.0, (!flag3 && !flag6) ? 0.5 : 0.0, (!flag4 && !flag6) ? 0.5 : 0.0, (!flag4 && !flag5) ? 0.5 : 0.0);
						break;
					}
					case ModuleShape.Bars:
					{
						bool flag = Keep(j, i - 1);
						bool flag2 = Keep(j, i + 1);
						pathBuilder.RoundedRect(new Rect(rect.X + 0.1, rect.Y - (flag ? 0.005 : 0.0), 0.8, 1.0 + (flag ? 0.005 : 0.0) + (flag2 ? 0.005 : 0.0)), flag ? 0.0 : 0.4, flag ? 0.0 : 0.4, flag2 ? 0.0 : 0.4, flag2 ? 0.0 : 0.4);
						break;
					}
					}
				}
			}
		}
		return pathBuilder.ToString();
		bool Keep(int x, int y)
		{
			if (!matrix.IsDark(x, y) || matrix.IsFinder(x, y))
			{
				return false;
			}
			if (clear.HasValue && clear.GetValueOrDefault().IntersectsWith(new Rect((double)x + 0.2, (double)y + 0.2, 0.6, 0.6)))
			{
				return false;
			}
			return true;
		}
	}

	public string EyePath(QrMatrix matrix)
	{
		PathBuilder pathBuilder = new PathBuilder();
		foreach (var (num, num2) in matrix.FinderOrigins)
		{
			AddEye(Eye, new Rect(num + Margin, num2 + Margin, 7.0, 7.0), pathBuilder);
		}
		return pathBuilder.ToString();
	}

	public static void AddEye(EyeShape eye, Rect outer, PathBuilder path)
	{
		Rect r = Inset(outer, 1.0);
		Rect r2 = Inset(outer, 2.0);
		switch (eye)
		{
		case EyeShape.Square:
			path.Rect(outer);
			path.Rect(r);
			path.Rect(r2);
			break;
		case EyeShape.Rounded:
			path.RoundedRect(outer, 2.0, 2.0, 2.0, 2.0);
			path.RoundedRect(r, 1.3, 1.3, 1.3, 1.3);
			path.RoundedRect(r2, 0.9, 0.9, 0.9, 0.9);
			break;
		case EyeShape.Circle:
			path.Ellipse(outer);
			path.Ellipse(r);
			path.Ellipse(r2);
			break;
		default:
			path.RoundedRect(outer, 3.0, 0.4, 3.0, 0.4);
			path.RoundedRect(r, 2.2, 0.2, 2.2, 0.2);
			path.RoundedRect(r2, 1.4, 0.0, 1.4, 0.0);
			break;
		}
	}

	public string? PlatePath(QrMatrix matrix)
	{
		(Rect, Rect)? tuple = LogoBox(matrix);
		if (tuple.HasValue)
		{
			(Rect, Rect) valueOrDefault = tuple.GetValueOrDefault();
			if (LogoPlate != Plate.None)
			{
				Rect item = valueOrDefault.Item2;
				item.Offset(Margin, Margin);
				item = Inset(item, 0.3);
				PathBuilder pathBuilder = new PathBuilder();
				switch (LogoPlate)
				{
				case Plate.Square:
					pathBuilder.Rect(item);
					break;
				case Plate.Rounded:
					pathBuilder.RoundedRect(item, item.Width * 0.2, item.Width * 0.2, item.Width * 0.2, item.Width * 0.2);
					break;
				default:
					pathBuilder.Ellipse(item);
					break;
				}
				return pathBuilder.ToString();
			}
		}
		return null;
	}

	public Rect? LogoRect(QrMatrix matrix)
	{
		if (Logo != null)
		{
			(Rect, Rect)? tuple = LogoBox(matrix);
			if (tuple.HasValue)
			{
				var (rect, _) = tuple.GetValueOrDefault();
				rect.Offset(Margin, Margin);
				if (LogoPlate == Plate.Circle)
				{
					rect = Inset(rect, rect.Width * 0.12);
				}
				double num = (double)Logo.PixelWidth / (double)Math.Max(1, Logo.PixelHeight);
				Size size = ((num >= 1.0) ? new Size(rect.Width, rect.Width / num) : new Size(rect.Height * num, rect.Height));
				return new Rect(rect.X + (rect.Width - size.Width) / 2.0, rect.Y + (rect.Height - size.Height) / 2.0, size.Width, size.Height);
			}
		}
		return null;
	}

	private static Rect Inset(Rect rect, double by)
	{
		return new Rect(rect.X + by, rect.Y + by, Math.Max(0.0, rect.Width - by * 2.0), Math.Max(0.0, rect.Height - by * 2.0));
	}

	public BitmapSource Image(QrMatrix matrix, int pixels)
	{
		int num = matrix.Size + Margin * 2;
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			drawingContext.PushTransform(new ScaleTransform((double)pixels / (double)num, (double)pixels / (double)num));
			Color? background = Background;
			if (background.HasValue)
			{
				Color valueOrDefault = background.GetValueOrDefault();
				drawingContext.DrawRectangle(new SolidColorBrush(valueOrDefault), null, new Rect(0.0, 0.0, num, num));
			}
			background = GradientEnd;
			object obj;
			if (background.HasValue)
			{
				Color valueOrDefault2 = background.GetValueOrDefault();
				obj = new LinearGradientBrush(Foreground, valueOrDefault2, new Point(0.0, 0.0), new Point(num, num))
				{
					MappingMode = BrushMappingMode.Absolute
				};
			}
			else
			{
				obj = new SolidColorBrush(Foreground);
			}
			Brush brush = (Brush)obj;
			Geometry geometry = Geometry.Parse("F1 " + ModulePath(matrix));
			drawingContext.DrawGeometry(brush, null, geometry);
			Geometry geometry2 = Geometry.Parse("F0 " + EyePath(matrix));
			drawingContext.DrawGeometry((EyeColor == Foreground) ? brush : new SolidColorBrush(EyeColor), null, geometry2);
			string text = PlatePath(matrix);
			if (text != null)
			{
				drawingContext.DrawGeometry(new SolidColorBrush(PlateColor), null, Geometry.Parse(text));
			}
			if (Logo != null)
			{
				Rect? rect = LogoRect(matrix);
				if (rect.HasValue)
				{
					Rect valueOrDefault3 = rect.GetValueOrDefault();
					drawingContext.DrawImage(Logo, valueOrDefault3);
				}
			}
			drawingContext.Pop();
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(pixels, pixels, 96.0, 96.0, PixelFormats.Pbgra32);
		RenderOptions.SetEdgeMode(drawingVisual, (Module == ModuleShape.Square && Eye == EyeShape.Square) ? EdgeMode.Aliased : EdgeMode.Unspecified);
		renderTargetBitmap.Render(drawingVisual);
		renderTargetBitmap.Freeze();
		return renderTargetBitmap;
	}

	public static byte[] Png(BitmapSource image)
	{
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(image));
		using MemoryStream memoryStream = new MemoryStream();
		pngBitmapEncoder.Save(memoryStream);
		return memoryStream.ToArray();
	}

	public string Svg(QrMatrix matrix)
	{
		int value = matrix.Size + Margin * 2;
		string value2 = "";
		string text = Hex(Foreground);
		Color? gradientEnd = GradientEnd;
		if (gradientEnd.HasValue)
		{
			Color valueOrDefault = gradientEnd.GetValueOrDefault();
			value2 = $"<defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"{value}\" y2=\"{value}\" gradientUnits=\"userSpaceOnUse\"><stop offset=\"0\" stop-color=\"{Hex(Foreground)}\"/><stop offset=\"1\" stop-color=\"{Hex(valueOrDefault)}\"/></linearGradient></defs>";
			text = "url(#g)";
		}
		string value3 = ((EyeColor == Foreground) ? text : Hex(EyeColor));
		StringBuilder stringBuilder = new StringBuilder();
		gradientEnd = Background;
		StringBuilder stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler;
		if (gradientEnd.HasValue)
		{
			Color valueOrDefault2 = gradientEnd.GetValueOrDefault();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(34, 3, stringBuilder2);
			handler.AppendLiteral("<rect width=\"");
			handler.AppendFormatted(value);
			handler.AppendLiteral("\" height=\"");
			handler.AppendFormatted(value);
			handler.AppendLiteral("\" fill=\"");
			handler.AppendFormatted(Hex(valueOrDefault2));
			handler.AppendLiteral("\"/>");
			stringBuilder3.Append(ref handler);
		}
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 2, stringBuilder2);
		handler.AppendLiteral("<path fill=\"");
		handler.AppendFormatted(text);
		handler.AppendLiteral("\" d=\"");
		handler.AppendFormatted(ModulePath(matrix));
		handler.AppendLiteral("\"/>");
		stringBuilder4.Append(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(40, 2, stringBuilder2);
		handler.AppendLiteral("<path fill=\"");
		handler.AppendFormatted(value3);
		handler.AppendLiteral("\" fill-rule=\"evenodd\" d=\"");
		handler.AppendFormatted(EyePath(matrix));
		handler.AppendLiteral("\"/>");
		stringBuilder5.Append(ref handler);
		string text2 = PlatePath(matrix);
		if (text2 != null)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(20, 2, stringBuilder2);
			handler.AppendLiteral("<path fill=\"");
			handler.AppendFormatted(Hex(PlateColor));
			handler.AppendLiteral("\" d=\"");
			handler.AppendFormatted(text2);
			handler.AppendLiteral("\"/>");
			stringBuilder6.Append(ref handler);
		}
		if (Logo != null)
		{
			Rect? rect = LogoRect(matrix);
			if (rect.HasValue)
			{
				Rect valueOrDefault3 = rect.GetValueOrDefault();
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(67, 5, stringBuilder2);
				handler.AppendLiteral("<image x=\"");
				handler.AppendFormatted(Num(valueOrDefault3.X));
				handler.AppendLiteral("\" y=\"");
				handler.AppendFormatted(Num(valueOrDefault3.Y));
				handler.AppendLiteral("\" width=\"");
				handler.AppendFormatted(Num(valueOrDefault3.Width));
				handler.AppendLiteral("\" height=\"");
				handler.AppendFormatted(Num(valueOrDefault3.Height));
				handler.AppendLiteral("\" href=\"data:image/png;base64,");
				handler.AppendFormatted(Convert.ToBase64String(Png(Logo)));
				handler.AppendLiteral("\"/>");
				stringBuilder7.Append(ref handler);
			}
		}
		return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {value} {value}\">{value2}{stringBuilder}</svg>\n";
	}

	public static string Hex(Color color)
	{
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	internal static string Num(double value)
	{
		string text = value.ToString("0.###", CultureInfo.InvariantCulture);
		if (!(text == "-0"))
		{
			return text;
		}
		return "0";
	}
}
