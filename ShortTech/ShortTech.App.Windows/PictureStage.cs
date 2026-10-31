using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShortTech.App.Windows;

public class PictureStage : Grid
{
	protected readonly Image Picture = new Image
	{
		Stretch = Stretch.Uniform
	};

	protected readonly Canvas Overlay = new Canvas
	{
		ClipToBounds = true
	};

	private readonly Border _frame;

	public BitmapSource? Source
	{
		get
		{
			return Picture.Source as BitmapSource;
		}
		set
		{
			Picture.Source = value;
			Layout();
		}
	}

	public Size Shown { get; private set; }

	public PictureStage(double height)
	{
		base.Height = height;
		base.Background = new SolidColorBrush(Color.FromRgb(30, 30, 32));
		base.FlowDirection = FlowDirection.LeftToRight;
		_frame = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid child = new Grid
		{
			Children = 
			{
				(UIElement)Picture,
				(UIElement)Overlay
			}
		};
		_frame.Child = child;
		base.Children.Add(_frame);
		base.SizeChanged += delegate
		{
			Layout();
		};
	}

	private void Layout()
	{
		if (Picture.Source is BitmapSource bitmapSource && !(base.ActualWidth <= 0.0))
		{
			double num = Math.Min((base.ActualWidth - 16.0) / (double)bitmapSource.PixelWidth, (base.ActualHeight - 16.0) / (double)bitmapSource.PixelHeight);
			Shown = new Size(Math.Max(1.0, (double)bitmapSource.PixelWidth * num), Math.Max(1.0, (double)bitmapSource.PixelHeight * num));
			_frame.Width = Shown.Width;
			_frame.Height = Shown.Height;
			Overlay.Width = Shown.Width;
			Overlay.Height = Shown.Height;
			OnLayout();
		}
	}

	protected virtual void OnLayout()
	{
	}

	public void SetUnderlay(UIElement element, bool hidePicture)
	{
		((Grid)_frame.Child).Children.Insert(0, element);
		Picture.Opacity = ((!hidePicture) ? 1 : 0);
	}

	public void ShowPicture(bool show)
	{
		Picture.Opacity = (show ? 1 : 0);
	}

	protected Point ToUnit(Point p)
	{
		return new Point(Math.Clamp(p.X / Shown.Width, 0.0, 1.0), Math.Clamp(p.Y / Shown.Height, 0.0, 1.0));
	}

	protected Point ToShown(double x, double y)
	{
		return new Point(x * Shown.Width, y * Shown.Height);
	}
}
