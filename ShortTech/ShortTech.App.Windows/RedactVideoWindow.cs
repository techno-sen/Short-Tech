using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.Core;
using ShortTech.Core.Images;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class RedactVideoWindow
{
	private sealed class Range
	{
		public double Start;

		public double End;
	}

	private sealed class RangeBar : FrameworkElement
	{
		private readonly MediaSession _media;

		private readonly Range _range;

		private readonly string _label;

		private readonly bool _selected;

		private readonly Action _pick;

		private int _edge;

		public RangeBar(MediaSession media, Range range, string label, bool selected, Action pick)
		{
			bool selected2 = selected;
			_media = media;
			_range = range;
			_label = label;
			_selected = selected2;
			_pick = pick;
			base.FlowDirection = FlowDirection.LeftToRight;
			base.Cursor = Cursors.Hand;
			media.Changed += base.InvalidateVisual;
		}

		private double X(double t)
		{
			return t / Math.Max(0.001, _media.Duration) * base.ActualWidth;
		}

		protected override void OnRender(DrawingContext dc)
		{
			double actualWidth = base.ActualWidth;
			double actualHeight = base.ActualHeight;
			double width = actualWidth;
			dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(13, 0, 0, 0)), null, new Rect(0.0, 0.0, width, actualHeight), 6.0, 6.0);
			double num = X(_range.Start);
			double num2 = X(_range.End);
			Rect rectangle = new Rect(num, 0.0, Math.Max(12.0, num2 - num), actualHeight);
			Color color = Color.FromArgb((byte)(_selected ? 191u : 115u), Theme.AccentColor.R, Theme.AccentColor.G, Theme.AccentColor.B);
			dc.DrawRoundedRectangle(new SolidColorBrush(color), null, rectangle, 6.0, 6.0);
			FormattedText formattedText = new FormattedText(_label, CultureInfo.CurrentUICulture, Theme.Direction, new Typeface(Theme.Font, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), 10.0, Brushes.White, 1.0);
			if (formattedText.Width < rectangle.Width - 16.0)
			{
				dc.DrawText(formattedText, new Point(rectangle.X + (rectangle.Width - formattedText.Width) / 2.0 + ((Theme.Direction == FlowDirection.RightToLeft) ? formattedText.Width : 0.0), (actualHeight - formattedText.Height) / 2.0));
			}
			dc.DrawRoundedRectangle(Brushes.White, null, new Rect(num, actualHeight / 2.0 - 8.0, 6.0, 16.0), 3.0, 3.0);
			dc.DrawRoundedRectangle(Brushes.White, null, new Rect(Math.Max(num + 6.0, num2 - 6.0), actualHeight / 2.0 - 8.0, 6.0, 16.0), 3.0, 3.0);
			double num3 = X(_media.Current);
			dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(179, 40, 43, 41)), null, new Rect(num3 - 0.5, 0.0, 1.0, actualHeight));
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			double x = e.GetPosition(this).X;
			_edge = ((Math.Abs(x - X(_range.Start) - 3.0) < 9.0) ? 1 : ((Math.Abs(x - X(_range.End) + 3.0) < 9.0) ? 2 : 0));
			if (_edge == 0)
			{
				_pick();
			}
			else
			{
				CaptureMouse();
			}
			e.Handled = true;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (_edge != 0 && e.LeftButton == MouseButtonState.Pressed)
			{
				double value = Math.Clamp(e.GetPosition(this).X / Math.Max(1.0, base.ActualWidth), 0.0, 1.0) * _media.Duration;
				if (_edge == 1)
				{
					_range.Start = Math.Clamp(value, 0.0, _range.End - 0.1);
				}
				else
				{
					_range.End = Math.Clamp(value, _range.Start + 0.1, _media.Duration);
				}
				_media.Seek((_edge == 1) ? _range.Start : _range.End);
				InvalidateVisual();
			}
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			_edge = 0;
			ReleaseMouseCapture();
		}
	}

	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build);
	}

	private static ToolWindow Build(MediaSession media)
	{
		BitmapSource source = Task.Run(() => media.Frame(0.0, 720)).GetAwaiter().GetResult() ?? throw new ShortTechException("Couldn't read a frame from " + Path.GetFileName(media.File));
		bool blur = false;
		Color color = Colors.Black;
		ToolWindow window = new ToolWindow("Redact Video", 580.0);
		BoxEditor editor = new BoxEditor(290.0)
		{
			Source = source
		};
		editor.Visible = delegate(BoxEditor.Box box)
		{
			Range range = RangeOf(box);
			return range.Start <= media.Current + 0.001 && media.Current <= range.End + 0.001;
		};
		if (media.Player != null)
		{
			MediaElement player = media.Player;
			editor.SetUnderlay(player, hidePicture: false);
			media.Opened += delegate
			{
				editor.ShowPicture(show: false);
			};
		}
		DispatcherTimer pending = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(100L)
		};
		pending.Tick += async delegate
		{
			pending.Stop();
			BitmapSource bitmapSource = await media.Frame(media.Current, 720);
			if (bitmapSource != null)
			{
				editor.Source = bitmapSource;
			}
		};
		Action rows_ = delegate
		{
		};
		Paint();
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(editor);
		Border control = ColorWell.Make(color, delegate(Color c)
		{
			color = c;
			Paint();
			editor.Draw();
		});
		Grid colorRow = UI.Row("Color", control, 34.0);
		stackPanel.Children.Add(UI.Row("Style", UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(false, "Solid"),
			(true, "Blur")
		}), selected: false, delegate(bool v)
		{
			blur = v;
			colorRow.Visibility = (v ? Visibility.Collapsed : Visibility.Visible);
			Paint();
			editor.Draw();
		}), 38.0).Pad(0.0, 8.0));
		stackPanel.Children.Add(colorRow);
		stackPanel.Children.Add(UI.Divider());
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		TextBlock element = UI.Text("Redactions", 12.0, FontWeights.SemiBold);
		DockPanel.SetDock(element, Dock.Left);
		dockPanel.Children.Add(element);
		Button trash = UI.IconButton("\ue74d", editor.DeleteSelected, "Delete Redaction", 28.0);
		Button whole = UI.Button("Show for Entire Video", delegate
		{
			BoxEditor.Box selected = editor.Selected;
			if (selected != null)
			{
				Range range = RangeOf(selected);
				range.Start = 0.0;
				range.End = media.Duration;
				rows_();
			}
		});
		whole.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		DockPanel.SetDock(trash, Dock.Right);
		DockPanel.SetDock(whole, Dock.Right);
		dockPanel.Children.Add(trash);
		dockPanel.Children.Add(whole);
		stackPanel.Children.Add(dockPanel);
		TextBlock empty = UI.Caption("Draw rectangles on the video to cover areas. Each one gets its own time range here.");
		stackPanel.Children.Add(empty.Pad(0.0, 6.0));
		StackPanel rows = new StackPanel
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 6.0)
		};
		stackPanel.Children.Add(rows);
		stackPanel.Children.Add(new Timeline(media, 36.0));
		stackPanel.Children.Add(MediaUI.Transport(media));
		Button save = UI.Button("Save Redacted Copy", delegate
		{
			List<VideoRedaction> redactions = editor.Boxes.Select(delegate(BoxEditor.Box b)
			{
				Range range = RangeOf(b);
				return new VideoRedaction(b.Rect.X, b.Rect.Y, b.Rect.Width, b.Rect.Height, range.Start, range.End);
			}).ToList();
			bool flag = blur;
			Rgb rgb = MediaUI.Rgb(color);
			Rgb rgb2 = rgb;
			bool useBlur = flag;
			window.Close();
			Runner.Run("Redact Video", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await MediaTools.Redact(f[0], redactions, useBlur, rgb2, c)));
		}, primary: true);
		rows_ = Rows;
		editor.Changed += Rows;
		editor.SelectionChanged += delegate
		{
			Button button = whole;
			bool isEnabled = (trash.IsEnabled = editor.Selected != null);
			button.IsEnabled = isEnabled;
		};
		media.Changed += delegate
		{
			editor.Draw();
			if (!media.IsOpen)
			{
				pending.Stop();
				pending.Start();
			}
		};
		window.Body = stackPanel;
		save.IsDefault = true;
		window.Trailing(save);
		Rows();
		return window;
		void Paint()
		{
			editor.Fill = (blur ? new SolidColorBrush(Color.FromArgb(89, byte.MaxValue, byte.MaxValue, byte.MaxValue)) : new SolidColorBrush(color));
		}
		Range RangeOf(BoxEditor.Box box)
		{
			object obj = box.Tag;
			if (obj == null)
			{
				Range obj2 = new Range
				{
					Start = 0.0,
					End = media.Duration
				};
				object obj3 = obj2;
				box.Tag = obj2;
				obj = obj3;
			}
			return (Range)obj;
		}
		void Rows()
		{
			rows.Children.Clear();
			for (int i = 0; i < editor.Boxes.Count; i++)
			{
				BoxEditor.Box box = editor.Boxes[i];
				Grid grid = new Grid
				{
					Height = 24.0,
					Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
				};
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(18.0)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				UIElementCollection children = grid.Children;
				string text = L10n.T($"{i + 1}");
				Brush muted = Theme.Muted;
				children.Add(UI.Text(text, 11.0, null, muted));
				RangeBar element2 = new RangeBar(media, RangeOf(box), L10n.T($"Box {i + 1}"), box == editor.Selected, delegate
				{
					editor.Select(box);
					media.Seek(RangeOf(box).Start);
				});
				Grid.SetColumn(element2, 1);
				grid.Children.Add(element2);
				rows.Children.Add(grid);
			}
			empty.Visibility = ((editor.Boxes.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
			Button button = whole;
			bool isEnabled = (trash.IsEnabled = editor.Selected != null);
			button.IsEnabled = isEnabled;
			save.IsEnabled = editor.Boxes.Count > 0;
			editor.Draw();
		}
	}
}
