using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class FramesWindow
{
	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build);
	}

	private static ToolWindow Build(MediaSession media)
	{
		List<(double Time, BitmapSource? Image)> frames = new List<(double, BitmapSource)>();
		ToolWindow window = new ToolWindow("Video Snapshots", 580.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MediaUI.Stage(media, 300.0));
		stackPanel.Children.Add(new Timeline(media, 44.0).Pad(0.0, 10.0));
		stackPanel.Children.Add(MediaUI.Transport(media));
		stackPanel.Children.Add(UI.Divider());
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		Brush muted = Theme.Muted;
		TextBlock count = UI.Text("", 11.0, null, muted);
		Button add = UI.Button("Add Frame", delegate
		{
		});
		DockPanel.SetDock(count, Dock.Left);
		DockPanel.SetDock(add, Dock.Right);
		dockPanel.Children.Add(count);
		dockPanel.Children.Add(add);
		stackPanel.Children.Add(dockPanel);
		StackPanel strip = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			FlowDirection = FlowDirection.LeftToRight
		};
		ScrollViewer scroller = new ScrollViewer
		{
			Content = strip,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(scroller);
		Button save = UI.Button("Save 1 Frame", delegate
		{
		}, primary: true);
		add.Click += async delegate
		{
			if (!Taken())
			{
				double time = media.Current;
				frames.Add((time, null));
				frames.Sort(((double Time, BitmapSource? Image) a, (double Time, BitmapSource? Image) b) => a.Time.CompareTo(b.Time));
				Build();
				BitmapSource item = await media.Frame(time, 120);
				int num = frames.FindIndex(((double Time, BitmapSource? Image) f) => f.Time == time);
				if (num >= 0)
				{
					frames[num] = (time, item);
					Build();
				}
			}
		};
		media.Changed += delegate
		{
			add.IsEnabled = !Taken();
		};
		save.Click += delegate
		{
			List<double> times = frames.Select<(double, BitmapSource), double>(((double Time, BitmapSource Image) f) => f.Time).ToList();
			window.Close();
			Runner.Run("Save Frames", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), (IReadOnlyList<string> f, OperationContext c) => MediaTools.SaveFrames(f[0], times, c));
		};
		window.Body = stackPanel;
		save.IsDefault = true;
		window.Trailing(save);
		Build();
		return window;
		void Build()
		{
			strip.Children.Clear();
			count.Text = L10n.T($"{frames.Count} selected");
			scroller.Visibility = ((frames.Count == 0) ? Visibility.Collapsed : Visibility.Visible);
			for (int i = 0; i < frames.Count; i++)
			{
				int index = i;
				(double Time, BitmapSource? Image) frame = frames[i];
				Grid grid = new Grid
				{
					Width = 96.0,
					Height = 60.0,
					Margin = new Thickness(0.0, 0.0, 8.0, 4.0),
					Cursor = Cursors.Hand
				};
				grid.Children.Add(new Border
				{
					CornerRadius = new CornerRadius(7.0),
					Background = ((frame.Image != null) ? ((Brush)new ImageBrush(frame.Image)
					{
						Stretch = Stretch.UniformToFill
					}) : ((Brush)new SolidColorBrush(Color.FromArgb(179, 0, 0, 0))))
				});
				Border element = new Border
				{
					CornerRadius = new CornerRadius(8.0),
					Background = new SolidColorBrush(Color.FromArgb(153, 0, 0, 0)),
					Padding = new Thickness(5.0, 1.0, 5.0, 1.0),
					Margin = new Thickness(4.0),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Bottom,
					Child = new TextBlock
					{
						Text = Format.Time(frame.Time),
						FontSize = 9.0,
						FontWeight = FontWeights.SemiBold,
						Foreground = Brushes.White
					}
				};
				grid.Children.Add(element);
				Border border = new Border
				{
					Width = 16.0,
					Height = 16.0,
					CornerRadius = new CornerRadius(8.0),
					Background = new SolidColorBrush(Color.FromArgb(166, 0, 0, 0)),
					Margin = new Thickness(4.0),
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					ToolTip = L10n.T("Remove selected frame"),
					Child = new TextBlock
					{
						Text = "\ue711",
						FontFamily = Theme.Icons,
						FontSize = 7.0,
						Foreground = Brushes.White,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				};
				border.MouseLeftButtonUp += delegate(object _, MouseButtonEventArgs e)
				{
					frames.RemoveAt(index);
					Build();
					e.Handled = true;
				};
				grid.Children.Add(border);
				grid.MouseLeftButtonUp += delegate
				{
					media.Seek(frame.Time);
				};
				strip.Children.Add(grid);
			}
			UIElementCollection children = ((StackPanel)save.Content).Children;
			((TextBlock)children[children.Count - 1]).Text = L10n.T((frames.Count == 1) ? "Save 1 Frame" : $"Save {frames.Count} Frames");
			save.IsEnabled = frames.Count > 0;
			add.IsEnabled = !Taken();
		}
		bool Taken()
		{
			return frames.Any<(double, BitmapSource)>(((double Time, BitmapSource Image) f) => Math.Abs(f.Time - media.Current) < media.FrameStep / 2.0);
		}
	}
}
