using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Images;

namespace ShortTech.App.Windows;

public static class MediaUI
{
	public static Border Stage(MediaSession media, double height)
	{
		Grid grid = new Grid
		{
			Background = Brushes.Black
		};
		if (media.Player != null)
		{
			grid.Children.Add(media.Player);
		}
		grid.Children.Add(media.Still);
		return new Border
		{
			Height = height,
			CornerRadius = new CornerRadius(12.0),
			Background = Brushes.Black,
			Child = grid,
			ClipToBounds = true
		};
	}

	public static Grid AudioHost(MediaSession media)
	{
		Grid grid = new Grid
		{
			Width = 1.0,
			Height = 1.0,
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		if (media.Player != null)
		{
			grid.Children.Add(media.Player);
		}
		return grid;
	}

	public static DockPanel Transport(MediaSession media, Action? play = null, bool frames = true)
	{
		DockPanel obj = new DockPanel
		{
			LastChildFill = false,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			FlowDirection = FlowDirection.LeftToRight
		};
		if (frames)
		{
			stackPanel.Children.Add(Plain("\ue892", delegate
			{
				media.Step(-1);
			}, "Previous frame"));
		}
		Button playButton = Plain("\ue768", play ?? ((Action)delegate
		{
			media.Play();
		}), "Play");
		stackPanel.Children.Add(playButton);
		if (frames)
		{
			stackPanel.Children.Add(Plain("\ue893", delegate
			{
				media.Step(1);
			}, "Next frame"));
		}
		DockPanel.SetDock(stackPanel, Dock.Left);
		obj.Children.Add(stackPanel);
		Brush muted = Theme.Muted;
		TextBlock time = UI.Text("", 11.0, null, muted);
		DockPanel.SetDock(time, Dock.Right);
		obj.Children.Add(time);
		media.Changed += Update;
		Update();
		return obj;
		static Button Plain(string glyph, Action click, string help)
		{
			Button button = UI.IconButton(glyph, click, help, 28.0);
			button.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			return button;
		}
		void Update()
		{
			time.Text = L10n.T(Format.Time(media.Current) + " / " + Format.Time(media.Duration));
			((TextBlock)playButton.Content).Text = (media.IsPlaying ? "\ue769" : "\ue768");
			playButton.IsEnabled = media.CanPlay;
		}
	}

	public static TextBox TimeField(double value, Action<double> set)
	{
		TextBox box = new TextBox
		{
			Width = 78.0,
			Height = 24.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			FontFamily = new FontFamily("Cascadia Mono, Consolas"),
			FontSize = 12.0,
			Text = Format.Time(value),
			FlowDirection = FlowDirection.LeftToRight
		};
		box.LostFocus += delegate
		{
			Commit();
		};
		box.KeyDown += delegate(object _, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				Commit();
			}
		};
		return box;
		void Commit()
		{
			double? num = Format.ParseTime(box.Text);
			if (num.HasValue)
			{
				double valueOrDefault = num.GetValueOrDefault();
				set(valueOrDefault);
			}
		}
	}

	public static Rgb Rgb(Color color)
	{
		return new Rgb((double)(int)color.R / 255.0, (double)(int)color.G / 255.0, (double)(int)color.B / 255.0);
	}

	public static Window? Open(IReadOnlyList<string> files, Func<MediaSession, ToolWindow> make, int thumbnails = 10, int bars = 240)
	{
		if (files.Count != 0)
		{
			MediaSession mediaSession = MediaSession.Load(files[0]);
			if (mediaSession != null)
			{
				ToolWindow toolWindow = make(mediaSession);
				mediaSession.Attach(toolWindow, thumbnails, bars);
				return toolWindow;
			}
		}
		return null;
	}
}
