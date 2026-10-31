using System;
using System.Windows;
using System.Windows.Media;

namespace ShortTech.App.Wheel;

public static class WheelGeometry
{
	public const double Size = 300.0;

	public const double OuterRadius = 119.5;

	public const double InnerRadius = 48.6;

	public static double LabelRadius => 84.05;

	public static Point Position(int index, int count)
	{
		double num = -Math.PI / 2.0 + (double)index / (double)Math.Max(count, 1) * 2.0 * Math.PI;
		return new Point(150.0 + LabelRadius * Math.Cos(num), 150.0 + LabelRadius * Math.Sin(num));
	}

	public static int? Index(Point point, int count)
	{
		if (count <= 0)
		{
			return null;
		}
		double num = point.X - 150.0;
		double num2 = point.Y - 150.0;
		double num3 = Math.Sqrt(num * num + num2 * num2);
		if (num3 <= 40.6 || num3 > 137.5)
		{
			return null;
		}
		double num4 = Math.Atan2(num2, num) + Math.PI / 2.0;
		if (num4 < 0.0)
		{
			num4 += Math.PI * 2.0;
		}
		double num5 = Math.PI * 2.0 / (double)count;
		return (int)((num4 + num5 / 2.0) / num5) % count;
	}

	public static Geometry Petal(int index, int count)
	{
		Point center = new Point(150.0, 150.0);
		if (count == 1)
		{
			GeometryGroup geometryGroup = new GeometryGroup();
			geometryGroup.FillRule = FillRule.EvenOdd;
			geometryGroup.Children.Add(new EllipseGeometry(center, 119.5, 119.5));
			geometryGroup.Children.Add(new EllipseGeometry(center, 48.6, 48.6));
			geometryGroup.Freeze();
			return geometryGroup;
		}
		double num = Math.PI * 2.0 / (double)count;
		double num2 = -Math.PI / 2.0 + (double)index * num;
		double num3 = num2 - num / 2.0;
		double num4 = num2 + num / 2.0;
		double num5 = num3 + Math.Asin(10.0 / 243.0);
		double num6 = num3 + Math.Asin(0.016736401673640166);
		double num7 = num4 - Math.Asin(0.016736401673640166);
		double num8 = num4 - Math.Asin(10.0 / 243.0);
		Point point = P(48.6, num5);
		Point point2 = P(119.5, num6);
		Point point3 = P(119.5, num7);
		Point point4 = P(48.6, num8);
		StreamGeometry streamGeometry = new StreamGeometry();
		StreamGeometryContext g = streamGeometry.Open();
		try
		{
			g.BeginFigure(Toward(point, point2, 7.0), isFilled: true, isClosed: true);
			g.LineTo(Toward(point2, point, 20.0), isStroked: true, isSmoothJoin: true);
			g.QuadraticBezierTo(point2, P(119.5, num6 + 0.16736401673640167), isStroked: true, isSmoothJoin: true);
			Arc(119.5, num6 + 0.16736401673640167, num7 - 0.16736401673640167);
			g.QuadraticBezierTo(point3, Toward(point3, point4, 20.0), isStroked: true, isSmoothJoin: true);
			g.LineTo(Toward(point4, point3, 7.0), isStroked: true, isSmoothJoin: true);
			g.QuadraticBezierTo(point4, P(48.6, num8 - 0.14403292181069957), isStroked: true, isSmoothJoin: true);
			Arc(48.6, num8 - 0.14403292181069957, num5 + 0.14403292181069957);
			g.QuadraticBezierTo(point, Toward(point, point2, 7.0), isStroked: true, isSmoothJoin: true);
		}
		finally
		{
			if (g != null)
			{
				((IDisposable)g).Dispose();
			}
		}
		streamGeometry.Freeze();
		return streamGeometry;
		void Arc(double radius, double a0, double a1)
		{
			for (int i = 1; i <= 28; i++)
			{
				g.LineTo(P(radius, a0 + (a1 - a0) * (double)i / 28.0), isStroked: true, isSmoothJoin: true);
			}
		}
		Point P(double radius, double angle)
		{
			return new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
		}
		static Point Toward(Point from, Point to, double distance)
		{
			double length = (to - from).Length;
			if (length <= 0.0)
			{
				return from;
			}
			double num9 = Math.Min(distance / length, 0.5);
			return new Point(from.X + (to.X - from.X) * num9, from.Y + (to.Y - from.Y) * num9);
		}
	}
}
