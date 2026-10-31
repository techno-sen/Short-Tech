using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShortTech.App.Windows;

public class Timeline : FrameworkElement
{
	protected readonly MediaSession Media;

	private bool _dragging;

	public Action<DrawingContext, double, double>? Overlay { get; set; }

	public Func<double, Point, bool>? Pressed { get; set; }

	public Action<double>? Dragged { get; set; }

	public Action? Released { get; set; }

	public float[]? Levels { get; set; }

	public Brush WaveBrush { get; set; } = new SolidColorBrush(Color.FromArgb(217, Theme.AccentColor.R, Theme.AccentColor.G, Theme.AccentColor.B));

	public bool ShowPlayhead { get; set; } = true;

	public Timeline(MediaSession media, double height)
	{
		Media = media;
		base.Height = height;
		base.FlowDirection = FlowDirection.LeftToRight;
		base.Cursor = Cursors.Hand;
		media.Changed += base.InvalidateVisual;
		media.StripLoaded += base.InvalidateVisual;
	}

	public double TimeAt(double x)
	{
		return Math.Clamp(x / Math.Max(1.0, base.ActualWidth), 0.0, 1.0) * Media.Duration;
	}

	public double XAt(double time)
	{
		return time / Math.Max(Media.Duration, 0.001) * base.ActualWidth;
	}

	protected override void OnRender(DrawingContext dc)
	{
		double actualWidth = base.ActualWidth;
		double actualHeight = base.ActualHeight;
		RectangleGeometry clipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, actualWidth, actualHeight), 8.0, 8.0);
		dc.PushClip(clipGeometry);
		float[] array = Levels ?? Media.Waveform;
		if (Media.Thumbnails.Count > 0)
		{
			dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(204, 0, 0, 0)), null, new Rect(0.0, 0.0, actualWidth, actualHeight));
			double num = actualWidth / 10.0;
			for (int i = 0; i < Media.Thumbnails.Count; i++)
			{
				BitmapSource bitmapSource = Media.Thumbnails[i];
				double num2 = Math.Max(num / (double)bitmapSource.PixelWidth, actualHeight / (double)bitmapSource.PixelHeight);
				Size size = new Size((double)bitmapSource.PixelWidth * num2, (double)bitmapSource.PixelHeight * num2);
				dc.PushClip(new RectangleGeometry(new Rect((double)i * num, 0.0, num, actualHeight)));
				dc.DrawImage(bitmapSource, new Rect((double)i * num + (num - size.Width) / 2.0, (actualHeight - size.Height) / 2.0, size.Width, size.Height));
				dc.Pop();
			}
		}
		else if (array.Length != 0)
		{
			dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, Theme.AccentColor.R, Theme.AccentColor.G, Theme.AccentColor.B)), null, new Rect(0.0, 0.0, actualWidth, actualHeight));
			DrawWave(dc, array, new Rect(0.0, 0.0, actualWidth, actualHeight), WaveBrush);
		}
		else
		{
			dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), null, new Rect(0.0, 0.0, actualWidth, actualHeight));
		}
		dc.Pop();
		Overlay?.Invoke(dc, actualWidth, actualHeight);
		if (ShowPlayhead)
		{
			double num3 = XAt(Media.Current);
			dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(217, 40, 43, 41)), null, new Rect(num3 - 0.75, 0.0, 1.5, actualHeight));
		}
	}

	public static void DrawWave(DrawingContext dc, float[] levels, Rect rect, Brush brush)
	{
		double num = rect.Width / (double)levels.Length;
		for (int i = 0; i < levels.Length; i++)
		{
			double num2 = Math.Max(1.5, (double)levels[i] * rect.Height * 0.85);
			dc.DrawRectangle(brush, null, new Rect(rect.X + (double)i * num, rect.Y + (rect.Height - num2) / 2.0, Math.Max(1.0, num * 0.55), num2));
		}
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		Point position = e.GetPosition(this);
		double num = TimeAt(position.X);
		Func<double, Point, bool>? pressed = Pressed;
		if (pressed == null || !pressed(num, position))
		{
			Media.Seek(num);
		}
		_dragging = true;
		CaptureMouse();
		e.Handled = true;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (_dragging)
		{
			double num = TimeAt(e.GetPosition(this).X);
			if (Dragged != null)
			{
				Dragged(num);
			}
			else
			{
				Media.Seek(num);
			}
			InvalidateVisual();
		}
	}

	protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
	{
		if (_dragging)
		{
			_dragging = false;
			ReleaseMouseCapture();
			Released?.Invoke();
			InvalidateVisual();
		}
	}
}
