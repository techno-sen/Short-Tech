using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ShortTech.Core;

public sealed class PathBuilder
{
	private readonly StringBuilder _d = new StringBuilder();

	private static string N(double v)
	{
		return QrDesign.Num(v);
	}

	public void Rect(Rect r)
	{
		StringBuilder d = _d;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 5, d);
		handler.AppendLiteral("M");
		handler.AppendFormatted(N(r.X));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(r.Y));
		handler.AppendLiteral("H");
		handler.AppendFormatted(N(r.Right));
		handler.AppendLiteral("V");
		handler.AppendFormatted(N(r.Bottom));
		handler.AppendLiteral("H");
		handler.AppendFormatted(N(r.X));
		handler.AppendLiteral("Z");
		d.Append(ref handler);
	}

	public void Polygon(IReadOnlyList<(double X, double Y)> points)
	{
		StringBuilder d = _d;
		StringBuilder stringBuilder = d;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, d);
		handler.AppendLiteral("M");
		handler.AppendFormatted(N(points[0].X));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(points[0].Y));
		stringBuilder.Append(ref handler);
		foreach (var item3 in points.Skip(1))
		{
			double item = item3.X;
			double item2 = item3.Y;
			d = _d;
			StringBuilder stringBuilder2 = d;
			handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, d);
			handler.AppendLiteral("L");
			handler.AppendFormatted(N(item));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(item2));
			stringBuilder2.Append(ref handler);
		}
		_d.Append('Z');
	}

	public void Ellipse(Rect r)
	{
		double num = r.Width / 2.0;
		double v = r.Height / 2.0;
		double v2 = r.X + num;
		StringBuilder d = _d;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 10, d);
		handler.AppendLiteral("M");
		handler.AppendFormatted(N(v2));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(r.Y));
		handler.AppendLiteral("A");
		handler.AppendFormatted(N(num));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(v));
		handler.AppendLiteral(" 0 1 1 ");
		handler.AppendFormatted(N(v2));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(r.Bottom));
		handler.AppendLiteral("A");
		handler.AppendFormatted(N(num));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(v));
		handler.AppendLiteral(" 0 1 1 ");
		handler.AppendFormatted(N(v2));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(r.Y));
		handler.AppendLiteral("Z");
		d.Append(ref handler);
	}

	public void RoundedRect(Rect r, double tl, double tr, double br, double bl)
	{
		double val = Math.Min(r.Width, r.Height) / 2.0;
		tl = Math.Min(tl, val);
		tr = Math.Min(tr, val);
		br = Math.Min(br, val);
		bl = Math.Min(bl, val);
		StringBuilder d = _d;
		StringBuilder stringBuilder = d;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 3, d);
		handler.AppendLiteral("M");
		handler.AppendFormatted(N(r.X + tl));
		handler.AppendLiteral(" ");
		handler.AppendFormatted(N(r.Y));
		handler.AppendLiteral("H");
		handler.AppendFormatted(N(r.Right - tr));
		stringBuilder.Append(ref handler);
		if (tr > 0.0)
		{
			d = _d;
			StringBuilder stringBuilder2 = d;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 4, d);
			handler.AppendLiteral("A");
			handler.AppendFormatted(N(tr));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(tr));
			handler.AppendLiteral(" 0 0 1 ");
			handler.AppendFormatted(N(r.Right));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(r.Y + tr));
			stringBuilder2.Append(ref handler);
		}
		d = _d;
		StringBuilder stringBuilder3 = d;
		handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, d);
		handler.AppendLiteral("V");
		handler.AppendFormatted(N(r.Bottom - br));
		stringBuilder3.Append(ref handler);
		if (br > 0.0)
		{
			d = _d;
			StringBuilder stringBuilder4 = d;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 4, d);
			handler.AppendLiteral("A");
			handler.AppendFormatted(N(br));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(br));
			handler.AppendLiteral(" 0 0 1 ");
			handler.AppendFormatted(N(r.Right - br));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(r.Bottom));
			stringBuilder4.Append(ref handler);
		}
		d = _d;
		StringBuilder stringBuilder5 = d;
		handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, d);
		handler.AppendLiteral("H");
		handler.AppendFormatted(N(r.X + bl));
		stringBuilder5.Append(ref handler);
		if (bl > 0.0)
		{
			d = _d;
			StringBuilder stringBuilder6 = d;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 4, d);
			handler.AppendLiteral("A");
			handler.AppendFormatted(N(bl));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(bl));
			handler.AppendLiteral(" 0 0 1 ");
			handler.AppendFormatted(N(r.X));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(r.Bottom - bl));
			stringBuilder6.Append(ref handler);
		}
		d = _d;
		StringBuilder stringBuilder7 = d;
		handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, d);
		handler.AppendLiteral("V");
		handler.AppendFormatted(N(r.Y + tl));
		stringBuilder7.Append(ref handler);
		if (tl > 0.0)
		{
			d = _d;
			StringBuilder stringBuilder8 = d;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 4, d);
			handler.AppendLiteral("A");
			handler.AppendFormatted(N(tl));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(tl));
			handler.AppendLiteral(" 0 0 1 ");
			handler.AppendFormatted(N(r.X + tl));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(N(r.Y));
			stringBuilder8.Append(ref handler);
		}
		_d.Append('Z');
	}

	public override string ToString()
	{
		return _d.ToString();
	}
}
