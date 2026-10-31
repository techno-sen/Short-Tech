using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public static class ChannelsWindow
{
	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build, 10, 160);
	}

	private static ToolWindow Build(MediaSession media)
	{
		bool mono = false;
		double left = 1.0;
		double right = 1.0;
		bool stereo = media.Info.AudioChannels >= 2;
		PreviewPlayer preview = new PreviewPlayer();
		ToolWindow window = new ToolWindow("Convert Audio Channels", 480.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MediaUI.AudioHost(media));
		UIElementCollection children = stackPanel.Children;
		Brush muted = Theme.Muted;
		children.Add(UI.Text("Output channel preview", 11.0, null, muted).Pad(0.0, 0.0));
		List<Timeline> waves = new List<Timeline>();
		List<Button> soloButtons = new List<Button>();
		for (int i = 0; i < ((!stereo) ? 1 : 2); i++)
		{
			int index = i;
			Grid grid = new Grid
			{
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(34.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(38.0)
			});
			Border element = new Border
			{
				Width = 24.0,
				Height = 24.0,
				CornerRadius = new CornerRadius(12.0),
				Background = Theme.AccentSoft,
				HorizontalAlignment = HorizontalAlignment.Left,
				Child = new TextBlock
				{
					Text = ((i == 0) ? (stereo ? "L" : "M") : "R"),
					FontSize = 11.0,
					FontWeight = FontWeights.SemiBold,
					Foreground = Theme.Ink,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				}
			};
			grid.Children.Add(element);
			Timeline timeline = new Timeline(media, 44.0)
			{
				Levels = Array.Empty<float>(),
				WaveBrush = new SolidColorBrush(Color.FromArgb(89, 0, 0, 0)),
				ShowPlayhead = false
			};
			timeline.Pressed = (double _, Point _) => true;
			timeline.Dragged = delegate
			{
			};
			Grid.SetColumn(timeline, 1);
			grid.Children.Add(timeline);
			waves.Add(timeline);
			Button button = UI.IconButton("\ue768", delegate
			{
				preview.Toggle($"solo{index}", () => MediaTools.PreviewAudio(media.File, MediaTools.ChannelFilter(media.Info, mono: true, 1.0, 1.0, index), complex: false));
			}, "Play this channel on its own", 28.0);
			button.HorizontalAlignment = HorizontalAlignment.Right;
			Grid.SetColumn(button, 2);
			grid.Children.Add(button);
			soloButtons.Add(button);
			stackPanel.Children.Add(grid);
		}
		stackPanel.Children.Add(UI.Divider());
		stackPanel.Children.Add(UI.Row("Output", UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(true, "Mono"),
			(false, "Stereo")
		}), selected: false, delegate(bool v)
		{
			mono = v;
		}), 36.0));
		stackPanel.Children.Add(UI.Row("Left volume", UI.Slider(0.0, 1.5, 1.0, delegate(double v)
		{
			left = v;
		}, Percent, null, 200.0)));
		stackPanel.Children.Add(UI.Row("Right volume", UI.Slider(0.0, 1.5, 1.0, delegate(double v)
		{
			right = v;
		}, Percent, null, 200.0)));
		Button playOutput = UI.Button("Play Output", delegate
		{
			preview.Toggle("output", () => MediaTools.PreviewAudio(media.File, MediaTools.ChannelFilter(media.Info, mono, left, right), complex: false));
		}, primary: false, "\ue768");
		stackPanel.Children.Add(UI.Row("Preview", playOutput, 36.0));
		preview.Changed += delegate
		{
			for (int j = 0; j < soloButtons.Count; j++)
			{
				((TextBlock)soloButtons[j].Content).Text = ((preview.Playing == $"solo{j}") ? "\ue71a" : "\ue768");
			}
			StackPanel obj = (StackPanel)playOutput.Content;
			((TextBlock)obj.Children[0]).Text = ((preview.Playing == "output") ? "\ue71a" : "\ue768");
			UIElementCollection children2 = obj.Children;
			((TextBlock)children2[children2.Count - 1]).Text = L10n.T((preview.Playing == "output") ? "Stop" : "Play Output");
		};
		window.Closed += delegate
		{
			preview.Stop();
		};
		window.Loaded += async delegate
		{
			Timeline timeline2 = waves[0];
			timeline2.Levels = await Task.Run(() => MediaOps.Waveform(media.File, 160, stereo ? new int?(0) : ((int?)null)));
			waves[0].InvalidateVisual();
			if (stereo)
			{
				timeline2 = waves[1];
				timeline2.Levels = await Task.Run(() => MediaOps.Waveform(media.File, 160, 1));
				waves[1].InvalidateVisual();
			}
		};
		window.Body = stackPanel;
		Button button2 = UI.Button("Apply", delegate
		{
			bool flag = mono;
			double num = left;
			double num2 = right;
			double r = num2;
			double l = num;
			bool m = flag;
			window.Close();
			Runner.Run("Convert Audio Channels", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await MediaTools.ConvertChannels(f[0], m, l, r, c)));
		}, primary: true);
		button2.IsDefault = true;
		window.Trailing(button2);
		return window;
		static string Percent(double v)
		{
			return L10n.T($"{Math.Round(v * 100.0)}%");
		}
	}
}
