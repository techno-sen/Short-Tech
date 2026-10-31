using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ShortTech.App.Windows;

public sealed class BoxEditor : PictureStage
{
	public sealed class Box
	{
		public Rect Rect;

		public Guid Id = Guid.NewGuid();

		public object? Tag;
	}

	private enum Mode
	{
		None,
		Draw,
		Move,
		Resize
	}

	private Point _down;

	private Rect _start;

	private Mode _mode;

	public List<Box> Boxes { get; } = new List<Box>();

	public Box? Selected { get; private set; }

	public Brush Fill { get; set; } = new SolidColorBrush(Color.FromArgb(204, 0, 0, 0));

	public Func<Box, bool> Visible { get; set; } = (Box _) => true;

	public event Action? Changed;

	public event Action? SelectionChanged;

	public BoxEditor(double height)
		: base(height)
	{
		Overlay.Background = Brushes.Transparent;
		Overlay.Cursor = Cursors.Cross;
		Overlay.MouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
		{
			Down(e);
		};
		Overlay.MouseMove += delegate(object _, MouseEventArgs e)
		{
			Move(e);
		};
		Overlay.MouseLeftButtonUp += delegate
		{
			if (_mode == Mode.Draw && Selected != null && (Selected.Rect.Width < 0.01 || Selected.Rect.Height < 0.01))
			{
				Boxes.Remove(Selected);
				Select(null);
			}
			if (_mode != Mode.None)
			{
				this.Changed?.Invoke();
			}
			_mode = Mode.None;
			Overlay.ReleaseMouseCapture();
			Draw();
		};
		base.Focusable = true;
		base.KeyDown += delegate(object _, KeyEventArgs e)
		{
			Key key = e.Key;
			if ((key == Key.Back || key == Key.Delete) ? true : false)
			{
				DeleteSelected();
			}
		};
	}

	public void DeleteSelected()
	{
		if (Selected != null)
		{
			Boxes.Remove(Selected);
			Select(null);
			this.Changed?.Invoke();
			Draw();
		}
	}

	public void Add(Rect rect, object? tag = null)
	{
		Box box = new Box
		{
			Rect = rect,
			Tag = tag
		};
		Boxes.Add(box);
		Select(box);
		this.Changed?.Invoke();
		Draw();
	}

	public void Select(Box? box)
	{
		Selected = box;
		this.SelectionChanged?.Invoke();
		Draw();
	}

	private void Down(MouseButtonEventArgs e)
	{
		Focus();
		Point p = ToUnit(e.GetPosition(Overlay));
		_down = p;
		Box box = Boxes.LastOrDefault((Box b) => Visible(b) && Inflate(b.Rect, 0.012).Contains(p));
		if (box != null)
		{
			Select(box);
			_start = box.Rect;
			Point point = new Point(box.Rect.Right, box.Rect.Bottom);
			_mode = ((Math.Abs(p.X - point.X) < 0.025 && Math.Abs(p.Y - point.Y) < 0.035) ? Mode.Resize : Mode.Move);
		}
		else
		{
			Box box2 = new Box
			{
				Rect = new Rect(p, p)
			};
			Boxes.Add(box2);
			Select(box2);
			_mode = Mode.Draw;
		}
		Overlay.CaptureMouse();
		e.Handled = true;
	}

	private static Rect Inflate(Rect r, double by)
	{
		r.Inflate(by, by);
		return r;
	}

	private void Move(MouseEventArgs e)
	{
		if (_mode != Mode.None && Selected != null)
		{
			Point point = ToUnit(e.GetPosition(Overlay));
			switch (_mode)
			{
			case Mode.Draw:
				Selected.Rect = new Rect(_down, point);
				break;
			case Mode.Move:
				Selected.Rect = new Rect(Math.Clamp(_start.X + point.X - _down.X, 0.0, 1.0 - _start.Width), Math.Clamp(_start.Y + point.Y - _down.Y, 0.0, 1.0 - _start.Height), _start.Width, _start.Height);
				break;
			case Mode.Resize:
				Selected.Rect = new Rect(_start.TopLeft, new Point(Math.Max(_start.X + 0.01, point.X), Math.Max(_start.Y + 0.01, point.Y)));
				break;
			}
			Draw();
		}
	}

	protected override void OnLayout()
	{
		Draw();
	}

	public void Draw()
	{
		Overlay.Children.Clear();
		foreach (Box item in Boxes.Where(Visible))
		{
			Point point = ToShown(item.Rect.Left, item.Rect.Top);
			Rectangle rectangle = new Rectangle
			{
				Width = Math.Max(1.0, item.Rect.Width * base.Shown.Width),
				Height = Math.Max(1.0, item.Rect.Height * base.Shown.Height),
				Fill = Fill,
				Stroke = ((item == Selected) ? Theme.Accent : Brushes.White),
				StrokeThickness = ((item != Selected) ? 1 : 2),
				IsHitTestVisible = false
			};
			Canvas.SetLeft(rectangle, point.X);
			Canvas.SetTop(rectangle, point.Y);
			Overlay.Children.Add(rectangle);
			if (item == Selected)
			{
				Rectangle element = new Rectangle
				{
					Width = 10.0,
					Height = 10.0,
					Fill = Theme.Accent,
					Stroke = Brushes.White,
					IsHitTestVisible = false
				};
				Canvas.SetLeft(element, point.X + rectangle.Width - 5.0);
				Canvas.SetTop(element, point.Y + rectangle.Height - 5.0);
				Overlay.Children.Add(element);
			}
		}
	}
}
