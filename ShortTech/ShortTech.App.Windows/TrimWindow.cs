using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class TrimWindow
{
	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build, 10, 220);
	}

	private static ToolWindow Build(MediaSession media)
	{
		bool hasVideo = media.Info.HasVideo;
		double start = 0.0;
		double end = media.Duration;
		bool silence = false;
		ToolWindow window = new ToolWindow(hasVideo ? "Trim Video" : "Trim Audio", hasVideo ? 580 : 500);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(hasVideo ? ((UIElement)MediaUI.Stage(media, 300.0)) : ((UIElement)MediaUI.AudioHost(media)));
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false,
			Margin = new Thickness(0.0, 10.0, 0.0, 6.0)
		};
		Brush muted = Theme.Muted;
		TextBlock element = UI.Text("Drag the ends to trim", 11.0, null, muted);
		muted = Theme.Muted;
		TextBlock length = UI.Text("", 11.0, null, muted);
		DockPanel.SetDock(element, Dock.Left);
		DockPanel.SetDock(length, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(length);
		stackPanel.Children.Add(dockPanel);
		Timeline timeline = new Timeline(media, hasVideo ? 64 : 96);
		TextBox startField = null;
		TextBox endField = null;
		SolidColorBrush dim = new SolidColorBrush(Color.FromArgb(166, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		timeline.Overlay = delegate(DrawingContext dc, double w, double h)
		{
			double num = timeline.XAt(start);
			double num2 = timeline.XAt(end);
			dc.DrawRectangle(dim, null, new Rect(0.0, 0.0, Math.Max(0.0, num), h));
			dc.DrawRectangle(dim, null, new Rect(num2, 0.0, Math.Max(0.0, w - num2), h));
			dc.DrawRoundedRectangle(null, new Pen(Theme.Accent, 3.0), new Rect(num + 1.5, 1.5, Math.Max(20.0, num2 - num) - 3.0, h - 3.0), 7.0, 7.0);
			double[] array = new double[2]
			{
				num,
				num2 - 12.0
			};
			foreach (double num3 in array)
			{
				dc.DrawRoundedRectangle(Theme.Accent, null, new Rect(num3, 0.0, 12.0, h), 4.0, 4.0);
				dc.DrawRoundedRectangle(Brushes.White, null, new Rect(num3 + 4.75, h / 2.0 - 9.0, 2.5, 18.0), 1.25, 1.25);
			}
		};
		int mode = 0;
		timeline.Pressed = delegate(double t, Point p)
		{
			double num = timeline.XAt(start);
			double num2 = timeline.XAt(end);
			mode = ((Math.Abs(p.X - num - 6.0) < 12.0) ? 1 : ((Math.Abs(p.X - num2 + 6.0) < 12.0) ? 2 : 0));
			return mode != 0;
		};
		timeline.Dragged = delegate(double t)
		{
			if (mode == 1)
			{
				SetStart(t);
			}
			else if (mode == 2)
			{
				SetEnd(t);
			}
			else
			{
				media.Seek(t);
			}
		};
		timeline.Released = delegate
		{
			mode = 0;
		};
		stackPanel.Children.Add(timeline);
		DockPanel dockPanel2 = new DockPanel
		{
			LastChildFill = false,
			Margin = new Thickness(0.0, 8.0, 0.0, 4.0)
		};
		startField = MediaUI.TimeField(start, SetStart);
		endField = MediaUI.TimeField(end, SetEnd);
		StackPanel element2 = Field("Start", startField);
		StackPanel element3 = Field("End", endField);
		DockPanel.SetDock(element2, Dock.Left);
		DockPanel.SetDock(element3, Dock.Right);
		dockPanel2.Children.Add(element2);
		dockPanel2.Children.Add(element3);
		stackPanel.Children.Add(dockPanel2);
		if (hasVideo)
		{
			stackPanel.Children.Add(MediaUI.Transport(media, PlaySelection));
		}
		else
		{
			stackPanel.Children.Add(UI.Check("Trim silent ends automatically", value: false, delegate(bool v)
			{
				silence = v;
			}).Pad(0.0, 4.0));
			Button play = UI.Button("Play Selected Range", PlaySelection, primary: false, "\ue768");
			play.HorizontalAlignment = HorizontalAlignment.Right;
			media.Changed += delegate
			{
				play.IsEnabled = media.CanPlay;
				StackPanel obj = (StackPanel)play.Content;
				((TextBlock)obj.Children[0]).Text = (media.IsPlaying ? "\ue71a" : "\ue768");
				UIElementCollection children = obj.Children;
				((TextBlock)children[children.Count - 1]).Text = L10n.T(media.IsPlaying ? "Stop" : "Play Selected Range");
			};
			play.IsEnabled = false;
			stackPanel.Children.Add(play);
		}
		window.Body = stackPanel;
		Button button = UI.Button("Apply", delegate
		{
			double num = start;
			double num2 = end;
			bool flag = silence;
			bool trim = flag;
			double e = num2;
			double s = num;
			window.Close();
			Runner.Run("Trim", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await MediaOps.Trim(f[0], s, e, trim, c)));
		}, primary: true);
		button.IsDefault = true;
		window.Trailing(button);
		window.KeyDown += delegate(object _, KeyEventArgs e)
		{
			if (e.Key == Key.Space && !(Keyboard.FocusedElement is TextBox))
			{
				PlaySelection();
				e.Handled = true;
			}
		};
		Update();
		return window;
		static StackPanel Field(string title, TextBox box)
		{
			StackPanel obj = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};
			UIElementCollection children = obj.Children;
			Brush muted2 = Theme.Muted;
			children.Add(UI.Text(title, 12.0, null, muted2).Pad(0.0, 0.0));
			obj.Children.Add(new Border
			{
				Width = 6.0
			});
			obj.Children.Add(box);
			return obj;
		}
		void PlaySelection()
		{
			if (media.IsPlaying)
			{
				media.Pause();
			}
			else
			{
				media.Play((media.Current < start || media.Current >= end - 0.05) ? new double?(start) : ((double?)null), end);
			}
		}
		void SetEnd(double value)
		{
			end = Math.Clamp(value, start + 0.1, media.Duration);
			media.Seek(end);
			Update();
		}
		void SetStart(double value)
		{
			start = Math.Clamp(value, 0.0, end - 0.1);
			media.Seek(start);
			Update();
		}
		void Update()
		{
			length.Text = L10n.T(Format.Time(end - start) + " selected");
			if (startField != null && !startField.IsKeyboardFocused)
			{
				startField.Text = Format.Time(start);
			}
			if (endField != null && !endField.IsKeyboardFocused)
			{
				endField.Text = Format.Time(end);
			}
			timeline.InvalidateVisual();
		}
	}
}
