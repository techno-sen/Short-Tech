using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class CropVideoWindow
{
	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build, 0);
	}

	private static ToolWindow Build(MediaSession media)
	{
		BitmapSource bitmapSource = Task.Run(() => media.Frame(0.0, 1080)).GetAwaiter().GetResult() ?? throw new ShortTechException("Couldn't read a frame from " + Path.GetFileName(media.File));
		double num = (double)Math.Max(media.Info.Width, media.Info.Height) / (double)Math.Max(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
		if (num <= 0.0)
		{
			num = 1.0;
		}
		int width = (int)Math.Round((double)bitmapSource.PixelWidth * num);
		int height = (int)Math.Round((double)bitmapSource.PixelHeight * num);
		ToolWindow window = new ToolWindow("Crop Video", 560.0);
		CropEditor editor = new CropEditor(360.0)
		{
			Source = bitmapSource,
			PixelWidth = width,
			PixelHeight = height
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(editor);
		TextBox w = new TextBox
		{
			Width = 70.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Right
		};
		TextBox h = new TextBox
		{
			Width = 70.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Right
		};
		bool updating = false;
		editor.Changed += ShowSize;
		w.LostFocus += delegate
		{
			Typed();
		};
		h.LostFocus += delegate
		{
			Typed();
		};
		stackPanel.Children.Add(UI.Row("Aspect ratio", UI.Picker(CropWindow.Ratios.Select(((double? Value, string Title) r) => (Value: r.Value, Title: r.Title)).ToList(), null, editor.SetRatio)).Pad(0.0, 10.0));
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			FlowDirection = FlowDirection.LeftToRight
		};
		UIElementCollection children = stackPanel2.Children;
		Brush muted = Theme.Muted;
		children.Add(UI.Text("W", 12.0, null, muted).Pad(4.0, 0.0));
		stackPanel2.Children.Add(w);
		UIElementCollection children2 = stackPanel2.Children;
		muted = Theme.Muted;
		children2.Add(UI.Text("H", 12.0, null, muted).Pad(8.0, 0.0));
		stackPanel2.Children.Add(h);
		UIElementCollection children3 = stackPanel2.Children;
		muted = Theme.Muted;
		children3.Add(UI.Text("px", 12.0, null, muted).Pad(6.0, 0.0));
		stackPanel.Children.Add(UI.Row("Size", stackPanel2));
		Slider scrub = new Slider
		{
			Minimum = 0.0,
			Maximum = Math.Max(media.Duration, 0.01),
			IsMoveToPointEnabled = true,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		muted = Theme.Muted;
		TextBlock time = UI.Text("", 11.0, null, muted);
		DispatcherTimer pending = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(120L)
		};
		pending.Tick += async delegate
		{
			pending.Stop();
			BitmapSource bitmapSource2 = await media.Frame(scrub.Value, 1080);
			if (bitmapSource2 != null)
			{
				editor.Source = bitmapSource2;
			}
		};
		scrub.ValueChanged += delegate
		{
			Moved();
		};
		stackPanel.Children.Add(scrub);
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			FlowDirection = FlowDirection.LeftToRight
		};
		stackPanel3.Children.Add(UI.IconButton("\ue892", delegate
		{
			scrub.Value = Math.Max(0.0, scrub.Value - media.FrameStep);
		}, "Previous frame", 28.0).Pad(0.0, 0.0));
		stackPanel3.Children.Add(new Border
		{
			Width = 8.0
		});
		stackPanel3.Children.Add(UI.IconButton("\ue893", delegate
		{
			scrub.Value = Math.Min(media.Duration, scrub.Value + media.FrameStep);
		}, "Next frame", 28.0));
		DockPanel.SetDock(stackPanel3, Dock.Left);
		DockPanel.SetDock(time, Dock.Right);
		dockPanel.Children.Add(stackPanel3);
		dockPanel.Children.Add(time);
		stackPanel.Children.Add(dockPanel);
		time.Text = L10n.T(Format.Time(0.0) + " / " + Format.Time(media.Duration));
		ShowSize();
		window.Body = stackPanel;
		window.Leading(UI.Button("Reset", delegate
		{
			editor.SetRatio(null);
			editor.SetCrop(new Rect(0.0, 0.0, 1.0, 1.0));
		}));
		Button button = UI.Button("Apply", delegate
		{
			Rect crop = editor.Crop;
			window.Close();
			Runner.Run("Crop Video", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), (IReadOnlyList<string> f, OperationContext c) => MediaTools.Crop(f, crop.X, crop.Y, crop.Width, crop.Height, c));
		}, primary: true);
		button.IsDefault = true;
		window.Trailing(button);
		return window;
		void Moved()
		{
			time.Text = L10n.T(Format.Time(scrub.Value) + " / " + Format.Time(media.Duration));
			pending.Stop();
			pending.Start();
		}
		void ShowSize()
		{
			updating = true;
			w.Text = Math.Round(editor.Crop.Width * (double)width).ToString(CultureInfo.InvariantCulture);
			h.Text = Math.Round(editor.Crop.Height * (double)height).ToString(CultureInfo.InvariantCulture);
			updating = false;
		}
		void Typed()
		{
			if (!updating && int.TryParse(w.Text, out var result) && int.TryParse(h.Text, out var result2) && result > 0 && result2 > 0)
			{
				double num2 = Math.Min(1.0, (double)result / (double)width);
				double num3 = Math.Min(1.0, (double)result2 / (double)height);
				editor.SetCrop(new Rect(Math.Min(editor.Crop.X, 1.0 - num2), Math.Min(editor.Crop.Y, 1.0 - num3), num2, num3));
			}
		}
	}
}
