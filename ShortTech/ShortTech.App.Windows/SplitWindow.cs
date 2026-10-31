using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class SplitWindow
{
	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build);
	}

	private static ToolWindow Build(MediaSession media)
	{
		List<double> points = new List<double>();
		HashSet<int> removed = new HashSet<int>();
		int? selected = null;
		int equalClips = 2;
		bool joinKept = true;
		Action refresh = delegate
		{
		};
		ToolWindow window = new ToolWindow("Split Video", 580.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MediaUI.Stage(media, 280.0));
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false,
			Margin = new Thickness(0.0, 10.0, 0.0, 6.0)
		};
		Brush muted = Theme.Muted;
		TextBlock count = UI.Text("", 11.0, null, muted);
		Button add = UI.Button("Add Split", delegate
		{
		}, primary: false, "\ue8c6");
		DockPanel.SetDock(count, Dock.Left);
		DockPanel.SetDock(add, Dock.Right);
		dockPanel.Children.Add(count);
		dockPanel.Children.Add(add);
		stackPanel.Children.Add(dockPanel);
		Timeline timeline = new Timeline(media, 56.0);
		SolidColorBrush fade = new SolidColorBrush(Color.FromArgb(191, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		timeline.Overlay = delegate(DrawingContext dc, double w, double h)
		{
			List<double> list = Bounds();
			for (int i = 0; i < list.Count - 1; i++)
			{
				double num = timeline.XAt(list[i]);
				double num2 = timeline.XAt(list[i + 1]);
				if (removed.Contains(i))
				{
					dc.DrawRectangle(fade, null, new Rect(num, 0.0, Math.Max(2.0, num2 - num), h));
				}
				if (selected == i)
				{
					dc.DrawRoundedRectangle(null, new Pen(Theme.Accent, 3.0), new Rect(num + 1.5, 1.5, Math.Max(2.0, num2 - num - 3.0), h - 3.0), 6.0, 6.0);
				}
			}
			foreach (double item in points)
			{
				double num3 = timeline.XAt(item);
				dc.DrawRoundedRectangle(Brushes.White, new Pen(Theme.Accent, 1.0), new Rect(num3 - 1.5, 0.0, 3.0, h), 1.5, 1.5);
			}
		};
		timeline.Pressed = delegate(double t, Point _)
		{
			List<double> list = Bounds();
			for (int i = 0; i < list.Count - 1; i++)
			{
				if (t >= list[i] && t <= list[i + 1])
				{
					selected = i;
					media.Seek(list[i]);
					break;
				}
			}
			refresh();
			return true;
		};
		timeline.Dragged = delegate
		{
		};
		stackPanel.Children.Add(timeline);
		stackPanel.Children.Add(MediaUI.Transport(media));
		stackPanel.Children.Add(UI.Divider());
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		TextBlock equalText = UI.Text("2", 13.0, FontWeights.SemiBold);
		equalText.MinWidth = 26.0;
		equalText.TextAlignment = TextAlignment.Center;
		stackPanel2.Children.Add(UI.IconButton("\ue738", delegate
		{
			equalClips = Math.Max(2, equalClips - 1);
			equalText.Text = equalClips.ToString();
		}, "Fewer", 24.0));
		stackPanel2.Children.Add(equalText);
		stackPanel2.Children.Add(UI.IconButton("\ue710", delegate
		{
			equalClips = Math.Min(50, equalClips + 1);
			equalText.Text = equalClips.ToString();
		}, "More", 24.0));
		stackPanel2.Children.Add(new Border
		{
			Width = 10.0
		});
		stackPanel2.Children.Add(UI.Button("Set", delegate
		{
			points = (from i in Enumerable.Range(1, equalClips - 1)
				select media.Duration * (double)i / (double)equalClips).ToList();
			removed.Clear();
			selected = null;
			refresh();
		}));
		stackPanel.Children.Add(UI.Row("Equal clips", stackPanel2, 34.0));
		Border join = UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(true, "Join the rest into one video"),
			(false, "Export separate clips")
		}), selected: true, delegate(bool v)
		{
			joinKept = v;
		});
		join.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		stackPanel.Children.Add(join);
		TextBlock hint = UI.Caption("");
		stackPanel.Children.Add(hint.Pad(0.0, 6.0));
		Button reset = UI.Button("Reset", delegate
		{
			points.Clear();
			removed.Clear();
			selected = null;
			refresh();
		});
		Button apply = UI.Button("Split Video", delegate
		{
			List<double> list = points.ToList();
			HashSet<int> hashSet = removed.ToHashSet();
			bool flag = joinKept && removed.Count > 0;
			bool j = flag;
			HashSet<int> r = hashSet;
			List<double> p = list;
			window.Close();
			Runner.Run("Split Video", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), (IReadOnlyList<string> f, OperationContext c) => MediaTools.Split(f[0], p, r, j, c));
		}, primary: true);
		refresh = Refresh;
		add.Click += delegate
		{
			if (CanSplit(media.Current))
			{
				points.Add(media.Current);
				removed.Clear();
				selected = null;
				Refresh();
			}
		};
		media.Changed += delegate
		{
			add.IsEnabled = CanSplit(media.Current);
		};
		window.KeyDown += delegate(object _, KeyEventArgs e)
		{
			Key key = e.Key;
			bool flag = ((key == Key.Back || key == Key.Delete) ? true : false);
			if (flag && selected.HasValue)
			{
				int valueOrDefault = selected.GetValueOrDefault();
				if (!removed.Remove(valueOrDefault))
				{
					removed.Add(valueOrDefault);
				}
				Refresh();
				e.Handled = true;
			}
			else if (e.Key == Key.Space)
			{
				media.Play();
				e.Handled = true;
			}
		};
		window.Body = stackPanel;
		window.Leading(reset);
		apply.IsDefault = true;
		window.Trailing(apply);
		Refresh();
		return window;
		List<double> Bounds()
		{
			List<double> list = new List<double>();
			list.Add(0.0);
			list.AddRange(points.OrderBy((double p) => p));
			list.Add(media.Duration);
			return list;
		}
		bool CanSplit(double t)
		{
			if (t > 0.05 && t < media.Duration - 0.05)
			{
				return points.All((double p) => Math.Abs(p - t) >= 0.05);
			}
			return false;
		}
		void Refresh()
		{
			count.Text = L10n.T((points.Count == 1) ? "1 split point" : $"{points.Count} split points");
			add.IsEnabled = CanSplit(media.Current);
			join.Visibility = ((removed.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
			TextBlock textBlock = hint;
			object english;
			if (selected.HasValue)
			{
				int valueOrDefault = selected.GetValueOrDefault();
				english = (removed.Contains(valueOrDefault) ? "Press Delete again to keep this clip." : "Press Delete to remove this clip.");
			}
			else
			{
				english = "Click a clip in the timeline, then press Delete to remove it. Space plays the video.";
			}
			textBlock.Text = L10n.T((string)english);
			reset.IsEnabled = points.Count > 0;
			apply.IsEnabled = points.Count > 0 && removed.Count < points.Count + 1;
			timeline.InvalidateVisual();
		}
	}
}
