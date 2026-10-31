using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShortTech.App.Windows;

public sealed class Annotation
{
	public required AnnotationTool Tool { get; init; }

	public Color Color { get; set; }

	public Point A { get; set; }

	public Point B { get; set; }

	public List<Point> Points { get; } = new List<Point>();

	public string Text { get; set; } = "";

	public int Number { get; set; }

	public BitmapSource? Image { get; set; }

	public Rect Bounds(double aspect)
	{
		switch (Tool)
		{
		case AnnotationTool.Draw:
			if (Points.Count > 0)
			{
				return Inflate(new Rect(new Point(Points.Min((Point p) => p.X), Points.Min((Point p) => p.Y)), new Point(Points.Max((Point p) => p.X), Points.Max((Point p) => p.Y))), 0.015);
			}
			break;
		case AnnotationTool.Text:
			return new Rect(A.X, A.Y, Math.Min((double)Math.Max(4, Text.Length) * 0.028 / Math.Max(aspect, 0.3), 1.0 - A.X), 0.07);
		case AnnotationTool.Callout:
			return new Rect(A.X - 0.035, A.Y - 0.035 * aspect, 0.07, 0.07 * aspect);
		case AnnotationTool.Magnifier:
			return new Rect(A.X - B.X, A.Y - B.X * aspect, B.X * 2.0, B.X * 2.0 * aspect);
		}
		return Inflate(new Rect(A, B), 0.012);
	}

	private static Rect Inflate(Rect r, double by)
	{
		r.Inflate(by, by);
		return r;
	}

	public void Move(Vector by)
	{
		A += by;
		AnnotationTool tool = Tool;
		if ((tool != AnnotationTool.Text && (uint)(tool - 6) > 1u) || 1 == 0)
		{
			B += by;
		}
		for (int i = 0; i < Points.Count; i++)
		{
			Points[i] += by;
		}
	}
}
