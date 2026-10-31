using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class SpeedWindow
{
	private static readonly double[] Marks = new double[6] { 0.25, 0.5, 1.0, 1.5, 2.0, 4.0 };

	public static double SpeedAt(double position)
	{
		double num = Math.Clamp(position, 0.0, Marks.Length - 1);
		int num2 = (int)Math.Floor(num);
		int num3 = Math.Min(num2 + 1, Marks.Length - 1);
		double num4 = Marks[num2] + (Marks[num3] - Marks[num2]) * (num - (double)num2);
		double[] marks = Marks;
		foreach (double num5 in marks)
		{
			if (Math.Abs(num5 - num4) < 0.04)
			{
				num4 = num5;
			}
		}
		return Math.Round(num4 * 100.0) / 100.0;
	}

	public static double PositionOf(double speed)
	{
		for (int i = 0; i < Marks.Length - 1; i++)
		{
			if (speed <= Marks[i + 1])
			{
				return (double)i + (speed - Marks[i]) / (Marks[i + 1] - Marks[i]);
			}
		}
		return Marks.Length - 1;
	}

	private static string X(double speed)
	{
		return speed.ToString("0.##", CultureInfo.InvariantCulture) + "×";
	}

	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build);
	}

	private static ToolWindow Build(MediaSession media)
	{
		bool hasVideo = media.Info.HasVideo;
		double speed = 1.0;
		ToolWindow window = new ToolWindow(hasVideo ? "Change Video Speed" : "Change Speed", 560.0);
		StackPanel stackPanel = new StackPanel();
		if (hasVideo)
		{
			stackPanel.Children.Add(MediaUI.Stage(media, 300.0));
		}
		else
		{
			stackPanel.Children.Add(MediaUI.AudioHost(media));
			stackPanel.Children.Add(new Timeline(media, 90.0));
		}
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false,
			Margin = new Thickness(0.0, 12.0, 0.0, 2.0)
		};
		TextBlock element = UI.Text("Playback speed", 12.0);
		TextBlock value = UI.Text(X(1.0), 13.0, FontWeights.SemiBold);
		DockPanel.SetDock(element, Dock.Left);
		DockPanel.SetDock(value, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(value);
		stackPanel.Children.Add(dockPanel);
		Slider slider = new Slider
		{
			Minimum = 0.0,
			Maximum = Marks.Length - 1,
			Value = PositionOf(1.0),
			IsMoveToPointEnabled = true,
			FlowDirection = FlowDirection.LeftToRight
		};
		stackPanel.Children.Add(slider);
		Grid marks = new Grid
		{
			FlowDirection = FlowDirection.LeftToRight,
			Height = 18.0
		};
		List<TextBlock> markLabels = new List<TextBlock>();
		for (int i = 0; i < Marks.Length; i++)
		{
			double mark = Marks[i];
			string text = X(mark);
			Brush muted = Theme.Muted;
			TextBlock textBlock = UI.Text(text, 10.0, null, muted);
			textBlock.HorizontalAlignment = HorizontalAlignment.Left;
			textBlock.Cursor = Cursors.Hand;
			textBlock.MouseLeftButtonUp += delegate
			{
				slider.Value = PositionOf(mark);
			};
			markLabels.Add(textBlock);
			marks.Children.Add(textBlock);
		}
		marks.SizeChanged += delegate
		{
			Place();
		};
		stackPanel.Children.Add(marks);
		stackPanel.Children.Add(MediaUI.Transport(media, delegate
		{
			MediaSession mediaSession = media;
			double rate = speed;
			mediaSession.Play(null, null, rate);
		}, frames: false).Pad(0.0, 6.0));
		TextBlock caption = UI.Caption("");
		stackPanel.Children.Add(caption);
		Button apply = UI.Button("Apply", delegate
		{
			double chosen = speed;
			window.Close();
			Runner.Run("Change Speed", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await MediaTools.ChangeSpeed(f[0], chosen, c)));
		}, primary: true);
		slider.ValueChanged += delegate
		{
			Update();
		};
		window.Body = stackPanel;
		apply.IsDefault = true;
		window.Trailing(apply);
		Update();
		return window;
		void Place()
		{
			for (int j = 0; j < markLabels.Count; j++)
			{
				double num = 5.5 + (marks.ActualWidth - 11.0) * (double)j / (double)(Marks.Length - 1);
				markLabels[j].Margin = new Thickness(0.0);
				markLabels[j].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				double left = Math.Clamp(num - markLabels[j].DesiredSize.Width / 2.0, 0.0, Math.Max(0.0, marks.ActualWidth - markLabels[j].DesiredSize.Width));
				markLabels[j].Margin = new Thickness(left, 0.0, 0.0, 0.0);
			}
		}
		void Update()
		{
			speed = SpeedAt(slider.Value);
			value.Text = X(speed);
			for (int j = 0; j < Marks.Length; j++)
			{
				bool flag = Math.Abs(Marks[j] - speed) < 0.001;
				markLabels[j].FontWeight = (flag ? FontWeights.Bold : FontWeights.Normal);
				markLabels[j].Foreground = (flag ? Theme.Ink : Theme.Muted);
			}
			caption.Text = L10n.T("Audio keeps its pitch. The new length is " + Format.Time(media.Duration / speed) + ".");
			apply.IsEnabled = Math.Abs(speed - 1.0) > 0.001;
			if (media.IsPlaying)
			{
				media.SetRate(speed);
			}
		}
	}
}
