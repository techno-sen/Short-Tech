using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class BleepWindow
{
	private sealed class Section
	{
		public double Start;

		public double End;
	}

	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build);
	}

	private static ToolWindow Build(MediaSession media)
	{
		List<Section> sections = new List<Section>();
		Section selected = null;
		PreviewPlayer preview = new PreviewPlayer();
		ToolWindow window = new ToolWindow("Bleep Audio", 540.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MediaUI.AudioHost(media));
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		stackPanel2.Children.Add(UI.Text("Bleeps", 12.0, FontWeights.SemiBold));
		Brush muted = Theme.Muted;
		TextBlock count = UI.Text("0", 12.0, null, muted).Pad(6.0, 0.0);
		stackPanel2.Children.Add(count);
		DockPanel.SetDock(stackPanel2, Dock.Left);
		dockPanel.Children.Add(stackPanel2);
		Button button = UI.Button("Bleep Entire Audio", delegate
		{
		});
		Button button2 = UI.Button("Add bleep at playhead", delegate
		{
		});
		button2.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		DockPanel.SetDock(button, Dock.Right);
		DockPanel.SetDock(button2, Dock.Right);
		dockPanel.Children.Add(button);
		dockPanel.Children.Add(button2);
		stackPanel.Children.Add(dockPanel);
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(48.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		StackPanel stackPanel3 = new StackPanel();
		UIElementCollection children = stackPanel3.Children;
		Border obj = new Border
		{
			Height = 36.0
		};
		muted = Theme.Muted;
		obj.Child = UI.Text("Bleep", 11.0, null, muted);
		children.Add(obj);
		UIElementCollection children2 = stackPanel3.Children;
		Border obj2 = new Border
		{
			Height = 76.0
		};
		muted = Theme.Muted;
		obj2.Child = UI.Text("Audio", 11.0, null, muted);
		children2.Add(obj2);
		grid.Children.Add(stackPanel3);
		StackPanel stackPanel4 = new StackPanel();
		Grid.SetColumn(stackPanel4, 1);
		grid.Children.Add(stackPanel4);
		Timeline layer = new Timeline(media, 30.0)
		{
			Levels = Array.Empty<float>(),
			ShowPlayhead = true,
			Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
		};
		stackPanel4.Children.Add(layer);
		Timeline element = new Timeline(media, 70.0);
		stackPanel4.Children.Add(element.Pad(0.0, 3.0));
		DockPanel dockPanel2 = new DockPanel
		{
			LastChildFill = false,
			FlowDirection = FlowDirection.LeftToRight
		};
		muted = Theme.Muted;
		TextBlock element2 = UI.Text("0:00.00", 10.0, null, muted);
		string text = Format.Time(media.Duration);
		muted = Theme.Muted;
		TextBlock element3 = UI.Text(text, 10.0, null, muted);
		DockPanel.SetDock(element2, Dock.Left);
		DockPanel.SetDock(element3, Dock.Right);
		dockPanel2.Children.Add(element2);
		dockPanel2.Children.Add(element3);
		stackPanel4.Children.Add(dockPanel2);
		stackPanel.Children.Add(grid);
		stackPanel.Children.Add(UI.Caption("Drag across the Bleep layer to add a section, then drag a section or either edge to adjust it. Delete removes the selected one.").Pad(0.0, 6.0));
		stackPanel.Children.Add(UI.Divider());
		Button original = UI.Button("Original", delegate
		{
			preview.Toggle("original", () => Task.FromResult(media.File));
		});
		Button bleeped = UI.Button("With Bleeps", delegate
		{
			List<(double Start, double End)> ranges = sections.Select((Section s) => (Start: s.Start, End: s.End)).ToList();
			preview.Toggle("bleeped", () => MediaTools.PreviewAudio(media.File, MediaTools.BleepFilter(ranges, media.Duration), complex: true));
		}, primary: false, "\ue768");
		StackPanel stackPanel5 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		stackPanel5.Children.Add(original);
		stackPanel5.Children.Add(new Border
		{
			Width = 6.0
		});
		stackPanel5.Children.Add(bleeped);
		stackPanel.Children.Add(UI.Row("Preview", stackPanel5, 36.0));
		Button reset = UI.Button("Reset", delegate
		{
		});
		Button save = UI.Button("Save Bleeped Copy", delegate
		{
		}, primary: true);
		SolidColorBrush trackBrush = new SolidColorBrush(Color.FromArgb(13, 0, 0, 0));
		layer.Overlay = delegate(DrawingContext dc, double w, double h)
		{
			dc.DrawRoundedRectangle(trackBrush, null, new Rect(0.0, 0.0, w, h), 7.0, 7.0);
			foreach (Section item in sections)
			{
				double num = layer.XAt(item.Start);
				double num2 = layer.XAt(item.End);
				Rect rectangle = new Rect(num, 0.0, Math.Max(8.0, num2 - num), h);
				Color color = Color.FromArgb((byte)((item == selected) ? 217u : 153u), Theme.AccentColor.R, Theme.AccentColor.G, Theme.AccentColor.B);
				dc.DrawRoundedRectangle(new SolidColorBrush(color), null, rectangle, 6.0, 6.0);
				dc.DrawRoundedRectangle(Brushes.White, null, new Rect(rectangle.X + 4.0, h / 2.0 - 7.0, 2.0, 14.0), 1.0, 1.0);
				dc.DrawRoundedRectangle(Brushes.White, null, new Rect(rectangle.Right - 6.0, h / 2.0 - 7.0, 2.0, 14.0), 1.0, 1.0);
				FormattedText formattedText = new FormattedText(L10n.T("Bleep"), CultureInfo.CurrentUICulture, Theme.Direction, new Typeface(Theme.Font, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), 10.0, Brushes.White, 1.0);
				if (formattedText.Width < rectangle.Width - 16.0)
				{
					dc.DrawText(formattedText, new Point(rectangle.X + (rectangle.Width - formattedText.Width) / 2.0 + ((Theme.Direction == FlowDirection.RightToLeft) ? formattedText.Width : 0.0), (h - formattedText.Height) / 2.0));
				}
			}
		};
		int mode = 0;
		double startTime = 0.0;
		(double Start, double End) origin = default((double, double));
		layer.Pressed = delegate(double t, Point p)
		{
			startTime = t;
			double edge = 6.0 * media.Duration / Math.Max(1.0, layer.ActualWidth);
			selected = sections.FirstOrDefault((Section s) => Math.Abs(s.Start - t) < edge);
			mode = 1;
			if (selected == null)
			{
				selected = sections.FirstOrDefault((Section s) => Math.Abs(s.End - t) < edge);
				mode = 2;
			}
			if (selected == null)
			{
				selected = sections.FirstOrDefault((Section s) => s.Start <= t && t <= s.End);
				mode = 0;
			}
			if (selected == null)
			{
				selected = new Section
				{
					Start = t,
					End = t
				};
				sections.Add(selected);
				mode = 3;
			}
			origin = (Start: selected.Start, End: selected.End);
			Refresh();
			return true;
		};
		layer.Dragged = delegate(double t)
		{
			if (selected != null)
			{
				double num = t - startTime;
				switch (mode)
				{
				case 0:
				{
					double num2 = origin.End - origin.Start;
					selected.Start = Math.Clamp(origin.Start + num, 0.0, media.Duration - num2);
					selected.End = selected.Start + num2;
					break;
				}
				case 1:
					selected.Start = Math.Min(t, origin.End - 0.05);
					break;
				case 2:
					selected.End = Math.Max(t, origin.Start + 0.05);
					break;
				default:
					selected.Start = Math.Min(startTime, t);
					selected.End = Math.Max(startTime, t);
					break;
				}
				Refresh();
			}
		};
		layer.Released = delegate
		{
			if (selected != null && selected.End - selected.Start < 0.05)
			{
				sections.Remove(selected);
				selected = null;
			}
			Refresh();
		};
		button2.Click += delegate
		{
			selected = new Section
			{
				Start = Math.Max(0.0, media.Current - 0.25),
				End = Math.Min(media.Duration, media.Current + 0.25)
			};
			sections.Add(selected);
			Refresh();
		};
		button.Click += delegate
		{
			sections.Clear();
			selected = new Section
			{
				Start = 0.0,
				End = media.Duration
			};
			sections.Add(selected);
			Refresh();
		};
		reset.Click += delegate
		{
			sections.Clear();
			selected = null;
			Refresh();
		};
		window.KeyDown += delegate(object _, KeyEventArgs e)
		{
			Key key = e.Key;
			bool flag = ((key == Key.Back || key == Key.Delete) ? true : false);
			if (flag && selected != null)
			{
				sections.Remove(selected);
				selected = null;
				Refresh();
				e.Handled = true;
			}
		};
		preview.Changed += delegate
		{
			UIElementCollection children3 = ((StackPanel)original.Content).Children;
			((TextBlock)children3[children3.Count - 1]).Text = L10n.T((preview.Playing == "original") ? "Stop" : "Original");
			StackPanel obj3 = (StackPanel)bleeped.Content;
			((TextBlock)obj3.Children[0]).Text = ((preview.Playing == "bleeped") ? "\ue71a" : "\ue768");
			UIElementCollection children4 = obj3.Children;
			((TextBlock)children4[children4.Count - 1]).Text = L10n.T((preview.Playing == "bleeped") ? "Stop" : "With Bleeps");
		};
		window.Closed += delegate
		{
			preview.Stop();
		};
		save.Click += delegate
		{
			List<(double Start, double End)> ranges = (from s in sections
				select (Start: s.Start, End: s.End) into r
				orderby r.Start
				select r).ToList();
			window.Close();
			Runner.Run("Bleep Audio", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await MediaTools.Bleep(f[0], ranges, c)));
		};
		window.Body = stackPanel;
		window.Leading(reset);
		save.IsDefault = true;
		window.Trailing(save);
		Refresh();
		return window;
		void Refresh()
		{
			count.Text = L10n.T($"{sections.Count}");
			Button button3 = reset;
			Button button4 = save;
			bool flag = (bleeped.IsEnabled = sections.Count > 0);
			bool isEnabled = (button4.IsEnabled = flag);
			button3.IsEnabled = isEnabled;
			layer.InvalidateVisual();
		}
	}
}
