using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShortTech.Core.Documents;

namespace ShortTech.App.Windows;

public static class AnnotationRenderer
{
	public static void Draw(DrawingContext dc, IReadOnlyList<Annotation> annotations, Size size, BitmapSource? baseImage)
	{
		double num = Math.Min(size.Width, size.Height);
		double num2 = Math.Max(2.0, num * 0.007);
		foreach (Annotation annotation in annotations)
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush(annotation.Color);
			Pen pen = new Pen(solidColorBrush, num2)
			{
				StartLineCap = PenLineCap.Round,
				EndLineCap = PenLineCap.Round,
				LineJoin = PenLineJoin.Round
			};
			switch (annotation.Tool)
			{
			case AnnotationTool.Arrow:
			{
				Point point = P(annotation.A);
				Point startPoint = P(annotation.B);
				double num5 = Math.Atan2(startPoint.Y - point.Y, startPoint.X - point.X);
				double num6 = num2 * 1.3;
				double num7 = Math.Max(num6 * 4.5, 10.0);
				dc.DrawLine(point1: new Point(startPoint.X - Math.Cos(num5) * num7 * 0.8, startPoint.Y - Math.Sin(num5) * num7 * 0.8), pen: new Pen(solidColorBrush, num6)
				{
					StartLineCap = PenLineCap.Round,
					EndLineCap = PenLineCap.Flat
				}, point0: point);
				StreamGeometry streamGeometry2 = new StreamGeometry();
				using (StreamGeometryContext streamGeometryContext2 = streamGeometry2.Open())
				{
					streamGeometryContext2.BeginFigure(startPoint, isFilled: true, isClosed: true);
					streamGeometryContext2.LineTo(new Point(startPoint.X - Math.Cos(num5 - 0.45) * num7, startPoint.Y - Math.Sin(num5 - 0.45) * num7), isStroked: true, isSmoothJoin: true);
					streamGeometryContext2.LineTo(new Point(startPoint.X - Math.Cos(num5 + 0.45) * num7, startPoint.Y - Math.Sin(num5 + 0.45) * num7), isStroked: true, isSmoothJoin: true);
				}
				dc.DrawGeometry(solidColorBrush, null, streamGeometry2);
				break;
			}
			case AnnotationTool.Highlight:
				dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(97, annotation.Color.R, annotation.Color.G, annotation.Color.B)), null, R(annotation.A, annotation.B));
				break;
			case AnnotationTool.Rectangle:
				dc.DrawRoundedRectangle(null, pen, R(annotation.A, annotation.B), num2, num2);
				break;
			case AnnotationTool.Ellipse:
			{
				Rect rect = R(annotation.A, annotation.B);
				dc.DrawEllipse(null, pen, new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0), rect.Width / 2.0, rect.Height / 2.0);
				break;
			}
			case AnnotationTool.Draw:
				if (annotation.Points.Count > 1)
				{
					StreamGeometry streamGeometry = new StreamGeometry();
					using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
					{
						streamGeometryContext.BeginFigure(P(annotation.Points[0]), isFilled: false, isClosed: false);
						streamGeometryContext.PolyLineTo(annotation.Points.Skip(1).Select(P).ToList(), isStroked: true, isSmoothJoin: true);
					}
					dc.DrawGeometry(null, pen, streamGeometry);
				}
				break;
			case AnnotationTool.Text:
				if (annotation.Text.Length > 0)
				{
					FormattedText formattedText2 = new FormattedText(annotation.Text, CultureInfo.CurrentUICulture, Bidi.IsRightToLeft(annotation.Text) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, new Typeface(Theme.Font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), num * 0.05, solidColorBrush, 1.0)
					{
						MaxTextWidth = Math.Max(num * 0.1, size.Width - P(annotation.A).X)
					};
					Point origin = P(annotation.A);
					if (formattedText2.FlowDirection == FlowDirection.RightToLeft)
					{
						origin.X += formattedText2.Width;
					}
					dc.DrawText(formattedText2, origin);
				}
				break;
			case AnnotationTool.Callout:
			{
				double num4 = num * 0.032;
				Point center2 = P(annotation.A);
				dc.DrawEllipse(solidColorBrush, null, center2, num4, num4);
				bool flag = (double)(annotation.Color.R + annotation.Color.G + annotation.Color.B) > 612.0;
				FormattedText formattedText = new FormattedText(annotation.Number.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Theme.Font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), num4 * 1.15, flag ? Brushes.Black : Brushes.White, 1.0);
				dc.DrawText(formattedText, new Point(center2.X - formattedText.Width / 2.0, center2.Y - formattedText.Height / 2.0));
				break;
			}
			case AnnotationTool.Magnifier:
				if (baseImage != null)
				{
					Point center = P(annotation.A);
					double num3 = annotation.B.X * size.Width;
					EllipseGeometry ellipseGeometry = new EllipseGeometry(center, num3, num3);
					dc.PushClip(ellipseGeometry);
					dc.DrawImage(baseImage, new Rect(center.X - center.X * 2.0, center.Y - center.Y * 2.0, size.Width * 2.0, size.Height * 2.0));
					dc.Pop();
					dc.DrawGeometry(null, new Pen(Brushes.White, num2 * 1.2), ellipseGeometry);
					dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)), num2 * 0.4), new EllipseGeometry(center, num3 + num2 * 0.8, num3 + num2 * 0.8));
				}
				break;
			case AnnotationTool.Image:
				if (annotation.Image != null)
				{
					dc.DrawImage(annotation.Image, R(annotation.A, annotation.B));
				}
				break;
			}
		}
		Point P(Point p)
		{
			return new Point(p.X * size.Width, p.Y * size.Height);
		}
		Rect R(Point a, Point b)
		{
			return new Rect(P(a), P(b));
		}
	}

	public static byte[] Render(BitmapSource photo, IReadOnlyList<Annotation> annotations)
	{
		Size size = new Size(photo.PixelWidth, photo.PixelHeight);
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			drawingContext.DrawImage(photo, new Rect(size));
			Draw(drawingContext, annotations, size, photo);
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(photo.PixelWidth, photo.PixelHeight, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(drawingVisual);
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
		using MemoryStream memoryStream = new MemoryStream();
		pngBitmapEncoder.Save(memoryStream);
		return memoryStream.ToArray();
	}
}
