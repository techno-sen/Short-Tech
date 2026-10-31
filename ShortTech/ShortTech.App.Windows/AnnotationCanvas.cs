using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace ShortTech.App.Windows;

public sealed class AnnotationCanvas : FrameworkElement
{
	private Point _down;

	private Annotation? _moving;

	private Annotation? _drawing;

	public BitmapSource? Photo { get; set; }

	public List<Annotation> Annotations { get; } = new List<Annotation>();

	public Annotation? Selected { get; set; }

	public AnnotationTool Tool { get; set; }

	public Color Color { get; set; } = Color.FromRgb(237, 66, 54);

	private double Aspect
	{
		get
		{
			if (Photo != null)
			{
				return (double)Photo.PixelWidth / (double)Photo.PixelHeight;
			}
			return 1.0;
		}
	}

	public event Action? SelectionChanged;

	public AnnotationCanvas()
	{
		base.Cursor = Cursors.Cross;
		base.Effect = new DropShadowEffect
		{
			BlurRadius = 6.0,
			ShadowDepth = 1.0,
			Opacity = 0.25
		};
	}

	protected override Size MeasureOverride(Size available)
	{
		if (Photo == null)
		{
			return new Size(0.0, 0.0);
		}
		double num = Math.Min(available.Width / (double)Photo.PixelWidth, available.Height / (double)Photo.PixelHeight);
		if (double.IsInfinity(num))
		{
			num = 1.0;
		}
		return new Size((double)Photo.PixelWidth * num, (double)Photo.PixelHeight * num);
	}

	protected override void OnRender(DrawingContext dc)
	{
		if (Photo != null)
		{
			Size renderSize = base.RenderSize;
			dc.DrawImage(Photo, new Rect(renderSize));
			AnnotationRenderer.Draw(dc, Annotations, renderSize, Photo);
			if (Selected != null)
			{
				Rect rect = Selected.Bounds(Aspect);
				dc.DrawRectangle(null, new Pen(Theme.Accent, 1.5)
				{
					DashStyle = DashStyles.Dash
				}, new Rect(rect.X * renderSize.Width, rect.Y * renderSize.Height, rect.Width * renderSize.Width, rect.Height * renderSize.Height));
			}
		}
	}

	private Point Unit(MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		return new Point(Math.Clamp(position.X / base.RenderSize.Width, 0.0, 1.0), Math.Clamp(position.Y / base.RenderSize.Height, 0.0, 1.0));
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		Focus();
		Point p = Unit(e);
		_down = p;
		Annotation annotation = Annotations.LastOrDefault((Annotation a) => a.Bounds(Aspect).Contains(p));
		if (annotation != null && Tool != AnnotationTool.Draw)
		{
			Selected = (_moving = annotation);
			this.SelectionChanged?.Invoke();
		}
		else
		{
			Annotation annotation2 = new Annotation
			{
				Tool = Tool,
				Color = Color,
				A = p,
				B = p
			};
			if (Tool == AnnotationTool.Draw)
			{
				annotation2.Points.Add(p);
			}
			if (Tool == AnnotationTool.Callout)
			{
				annotation2.Number = Annotations.Count((Annotation a) => a.Tool == AnnotationTool.Callout) + 1;
			}
			if (Tool == AnnotationTool.Magnifier)
			{
				annotation2.B = new Point(0.0, 0.0);
			}
			if (Tool == AnnotationTool.Image)
			{
				return;
			}
			Annotations.Add(annotation2);
			Selected = (_drawing = annotation2);
			this.SelectionChanged?.Invoke();
		}
		CaptureMouse();
		InvalidateVisual();
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		Point point = Unit(e);
		if (_moving != null)
		{
			_moving.Move(point - _down);
			_down = point;
		}
		else if (_drawing != null)
		{
			switch (_drawing.Tool)
			{
			case AnnotationTool.Draw:
				_drawing.Points.Add(point);
				break;
			case AnnotationTool.Magnifier:
				_drawing.B = new Point(Math.Max(0.03, (point - _drawing.A).Length), 0.0);
				break;
			case AnnotationTool.Text:
			case AnnotationTool.Callout:
				_drawing.A = point;
				break;
			default:
				_drawing.B = point;
				break;
			}
		}
		InvalidateVisual();
	}

	protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
	{
		Annotation drawing = _drawing;
		if (drawing != null)
		{
			AnnotationTool tool = drawing.Tool;
			bool flag = (((uint)tool <= 1u || (uint)(tool - 3) <= 1u) ? true : false);
			bool num = flag && (drawing.B - drawing.A).Length < 0.01;
			if (drawing.Tool == AnnotationTool.Magnifier && drawing.B.X < 0.03)
			{
				drawing.B = new Point(0.12, 0.0);
			}
			if (num)
			{
				Annotations.Remove(drawing);
				Selected = null;
			}
		}
		_drawing = (_moving = null);
		ReleaseMouseCapture();
		this.SelectionChanged?.Invoke();
		InvalidateVisual();
	}
}
