using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShortTech.App.Windows;

public sealed class CropEditor : PictureStage
{
	private readonly Rectangle _frameLine = new Rectangle
	{
		Stroke = Brushes.White,
		StrokeThickness = 1.5,
		Fill = Brushes.Transparent,
		Cursor = Cursors.SizeAll
	};

	private readonly Path _shade = new Path
	{
		Fill = new SolidColorBrush(Color.FromArgb(136, 0, 0, 0)),
		IsHitTestVisible = false
	};

	private readonly List<Rectangle> _handles = new List<Rectangle>();

	private Rect _start;

	private Point _down;

	private int _dragging = -2;

	public Rect Crop { get; private set; } = new Rect(0.0, 0.0, 1.0, 1.0);

	public double? Ratio { get; private set; }

	public int PixelWidth { get; set; } = 1;

	public int PixelHeight { get; set; } = 1;

	public event Action? Changed;

	public CropEditor(double height)
		: base(height)
	{
		Overlay.Children.Add(_shade);
		Overlay.Children.Add(_frameLine);
		for (int i = 0; i < 4; i++)
		{
			Rectangle rectangle = new Rectangle
			{
				Width = 12.0,
				Height = 12.0,
				Fill = Theme.Accent,
				Stroke = Brushes.White,
				StrokeThickness = 1.5,
				Cursor = ((i % 3 == 0) ? Cursors.SizeNWSE : Cursors.SizeNESW)
			};
			int index = i;
			rectangle.MouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
			{
				Begin(index, e);
			};
			_handles.Add(rectangle);
			Overlay.Children.Add(rectangle);
		}
		_frameLine.MouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
		{
			Begin(-1, e);
		};
		Overlay.MouseMove += delegate(object _, MouseEventArgs e)
		{
			Drag(e);
		};
		Overlay.MouseLeftButtonUp += delegate
		{
			_dragging = -2;
			Overlay.ReleaseMouseCapture();
		};
	}

	public void SetRatio(double? ratio)
	{
		Ratio = ratio;
		if (ratio.HasValue)
		{
			double valueOrDefault = ratio.GetValueOrDefault();
			double num = (double)PixelWidth / (double)PixelHeight;
			double num2 = 1.0;
			double num3 = 1.0;
			if (valueOrDefault > num)
			{
				num3 = num / valueOrDefault;
			}
			else
			{
				num2 = valueOrDefault / num;
			}
			Crop = new Rect((1.0 - num2) / 2.0, (1.0 - num3) / 2.0, num2, num3);
		}
		Draw();
		this.Changed?.Invoke();
	}

	public void SetCrop(Rect crop)
	{
		Crop = crop;
		Draw();
		this.Changed?.Invoke();
	}

	private void Begin(int index, MouseButtonEventArgs e)
	{
		_dragging = index;
		_start = Crop;
		_down = ToUnit(e.GetPosition(Overlay));
		Overlay.CaptureMouse();
		e.Handled = true;
	}

	private void Drag(MouseEventArgs e)
	{
		if (_dragging == -2 || e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		Point point = ToUnit(e.GetPosition(Overlay));
		double num = point.X - _down.X;
		double num2 = point.Y - _down.Y;
		Rect crop = _start;
		if (_dragging == -1)
		{
			crop.X = Math.Clamp(_start.X + num, 0.0, 1.0 - crop.Width);
			crop.Y = Math.Clamp(_start.Y + num2, 0.0, 1.0 - crop.Height);
		}
		else
		{
			double num3 = _start.Left;
			double num4 = _start.Top;
			double num5 = _start.Right;
			double num6 = _start.Bottom;
			int dragging = _dragging;
			if ((dragging == 0 || dragging == 2) ? true : false)
			{
				num3 = Math.Clamp(num3 + num, 0.0, num5 - 0.03);
			}
			else
			{
				num5 = Math.Clamp(num5 + num, num3 + 0.03, 1.0);
			}
			dragging = _dragging;
			if ((uint)dragging <= 1u)
			{
				num4 = Math.Clamp(num4 + num2, 0.0, num6 - 0.03);
			}
			else
			{
				num6 = Math.Clamp(num6 + num2, num4 + 0.03, 1.0);
			}
			crop = new Rect(new Point(num3, num4), new Point(num5, num6));
			double? ratio = Ratio;
			if (ratio.HasValue)
			{
				double valueOrDefault = ratio.GetValueOrDefault();
				double num7 = valueOrDefault * (double)PixelHeight / (double)PixelWidth;
				double num8 = crop.Width / num7;
				dragging = _dragging;
				bool flag = (uint)dragging <= 1u;
				crop = ((!flag) ? new Rect(crop.Left, crop.Top, crop.Width, Math.Min(num8, 1.0 - crop.Top)) : new Rect(crop.Left, Math.Max(0.0, crop.Bottom - num8), crop.Width, Math.Min(num8, crop.Bottom)));
				crop.Width = crop.Height * num7;
				dragging = _dragging;
				if ((dragging == 0 || dragging == 2) ? true : false)
				{
					crop.X = _start.Right - crop.Width;
				}
			}
		}
		Crop = crop;
		Draw();
		this.Changed?.Invoke();
	}

	protected override void OnLayout()
	{
		Draw();
	}

	private void Draw()
	{
		if (!(base.Shown.Width <= 1.0))
		{
			Point point = ToShown(Crop.Left, Crop.Top);
			Point point2 = ToShown(Crop.Right, Crop.Bottom);
			Canvas.SetLeft(_frameLine, point.X);
			Canvas.SetTop(_frameLine, point.Y);
			_frameLine.Width = Math.Max(1.0, point2.X - point.X);
			_frameLine.Height = Math.Max(1.0, point2.Y - point.Y);
			GeometryGroup geometryGroup = new GeometryGroup
			{
				FillRule = FillRule.EvenOdd
			};
			geometryGroup.Children.Add(new RectangleGeometry(new Rect(0.0, 0.0, base.Shown.Width, base.Shown.Height)));
			geometryGroup.Children.Add(new RectangleGeometry(new Rect(point, point2)));
			_shade.Data = geometryGroup;
			Point[] array = new Point[4]
			{
				point,
				new Point(point2.X, point.Y),
				new Point(point.X, point2.Y),
				point2
			};
			for (int i = 0; i < 4; i++)
			{
				Canvas.SetLeft(_handles[i], array[i].X - 6.0);
				Canvas.SetTop(_handles[i], array[i].Y - 6.0);
			}
		}
	}
}
