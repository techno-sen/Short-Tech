using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class NormalizeWindow
{
	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build);
	}

	private static ToolWindow Build(MediaSession media)
	{
		double target = -16.0;
		double range = 11.0;
		double peak = -1.5;
		ToolWindow window = new ToolWindow("Normalize Volume", 480.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MediaUI.AudioHost(media));
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		Brush muted = Theme.Muted;
		TextBlock textBlock = UI.Text("", 11.0, null, muted);
		textBlock.Text = Path.GetFileName(media.File);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		textBlock.MaxWidth = 340.0;
		string text = Format.Time(media.Duration);
		muted = Theme.Muted;
		TextBlock element = UI.Text(text, 11.0, null, muted);
		DockPanel.SetDock(textBlock, Dock.Left);
		DockPanel.SetDock(element, Dock.Right);
		dockPanel.Children.Add(textBlock);
		dockPanel.Children.Add(element);
		stackPanel.Children.Add(dockPanel);
		Timeline timeline = new Timeline(media, 150.0)
		{
			Margin = new Thickness(0.0, 8.0, 0.0, 4.0)
		};
		if (media.Info.HasVideo)
		{
			timeline.Levels = Array.Empty<float>();
		}
		stackPanel.Children.Add(timeline);
		stackPanel.Children.Add(MediaUI.Transport(media, null, frames: false));
		muted = Theme.Muted;
		TextBlock measured = UI.Text("", 11.0, null, muted);
		measured.Visibility = Visibility.Collapsed;
		stackPanel.Children.Add(measured.Pad(0.0, 6.0));
		List<Slider> sliders = new List<Slider>();
		stackPanel.Children.Add(Setting("Target loudness", target, -24.0, -5.0, 1.0, "LUFS", delegate(double v)
		{
			target = v;
		}));
		stackPanel.Children.Add(Setting("Loudness range", range, 1.0, 20.0, 1.0, "LU", delegate(double v)
		{
			range = v;
		}));
		stackPanel.Children.Add(Setting("True peak limit", peak, -9.0, 0.0, 0.5, "dBTP", delegate(double v)
		{
			peak = v;
		}));
		stackPanel.Children.Add(UI.Caption("−16 LUFS is a balanced default for spoken audio and music. The true peak limit helps prevent clipping after encoding.").Pad(0.0, 10.0));
		window.Body = stackPanel;
		window.Leading(UI.Button("Restore Defaults", delegate
		{
			sliders[0].Value = -16.0;
			sliders[1].Value = 11.0;
			sliders[2].Value = -1.5;
		}));
		Button button = UI.Button("Normalize Audio", delegate
		{
			double num = target;
			double num2 = range;
			double num3 = peak;
			double p = num3;
			double r = num2;
			double t = num;
			window.Close();
			Runner.Run("Normalize Volume", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), (IReadOnlyList<string> f, OperationContext c) => MediaOps.Normalize(f, t, r, p, c));
		}, primary: true);
		button.IsDefault = true;
		window.Trailing(button);
		window.Loaded += async delegate
		{
			MediaOps.Loudness loudness = await Task.Run(() => MediaOps.MeasureLoudness(media.File));
			if ((object)loudness != null)
			{
				measured.Text = L10n.T($"Now: {loudness.I} LUFS · peak {loudness.Tp} dBTP · range {loudness.Lra} LU");
				measured.Visibility = Visibility.Visible;
			}
		};
		return window;
		StackPanel Setting(string title, double value, double min, double max, double step, string unit, Action<double> set)
		{
			StackPanel obj = new StackPanel
			{
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			};
			DockPanel dockPanel2 = new DockPanel
			{
				LastChildFill = false
			};
			TextBlock element2 = UI.Text(title, 12.0);
			TextBlock shown = UI.Text(Show(value), 12.0);
			DockPanel.SetDock(element2, Dock.Left);
			DockPanel.SetDock(shown, Dock.Right);
			dockPanel2.Children.Add(element2);
			dockPanel2.Children.Add(shown);
			Slider slider = new Slider
			{
				Minimum = min,
				Maximum = max,
				Value = value,
				TickFrequency = step,
				IsSnapToTickEnabled = true,
				IsMoveToPointEnabled = true,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			};
			slider.ValueChanged += delegate(object _, RoutedPropertyChangedEventArgs<double> e)
			{
				set(e.NewValue);
				shown.Text = Show(e.NewValue);
			};
			sliders.Add(slider);
			obj.Children.Add(dockPanel2);
			obj.Children.Add(slider);
			return obj;
			string Show(double v)
			{
				return v.ToString((step < 1.0) ? "0.0" : "0", CultureInfo.InvariantCulture) + " " + unit;
			}
		}
	}
}
