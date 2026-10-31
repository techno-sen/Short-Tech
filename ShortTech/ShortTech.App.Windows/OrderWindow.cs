using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Media;
using ShortTech.Core.Pdf;

namespace ShortTech.App.Windows;

public static class OrderWindow
{
	public static Window MergePdfs(IReadOnlyList<string> files)
	{
		return Make("Merge PDFs", "Document order", files, delegate(IReadOnlyList<string> order)
		{
			Runner.Run("Merge PDFs", order, PdfOps.Merge);
		});
	}

	public static Window JoinVideos(IReadOnlyList<string> files)
	{
		return Make("Join Videos", "Playback order", files, delegate(IReadOnlyList<string> order)
		{
			Runner.Run("Join Videos", order, MediaOps.Join);
		});
	}

	public static Window JoinAudio(IReadOnlyList<string> files)
	{
		return Make("Join Audio", "Playback order", files, delegate(IReadOnlyList<string> order)
		{
			Runner.Run("Join Audio", order, MediaOps.JoinAudio);
		});
	}

	private static Window Make(string title, string caption, IReadOnlyList<string> files, Action<IReadOnlyList<string>> done)
	{
		List<string> items = files.ToList();
		Dictionary<string, ImageSource> thumbnails = files.Distinct().ToDictionary((string f) => f, (string f) => ShellThumbnail.For(f, 64));
		ToolWindow window = new ToolWindow(title, 400.0);
		StackPanel list = new StackPanel();
		Build();
		StackPanel stackPanel = new StackPanel();
		UIElementCollection children = stackPanel.Children;
		Brush muted = Theme.Muted;
		children.Add(UI.Text(caption, 12.0, null, muted).Pad(0.0, 0.0));
		stackPanel.Children.Add(new ScrollViewer
		{
			Content = list,
			MaxHeight = 340.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		});
		window.Body = stackPanel;
		Button button = UI.Button(title, delegate
		{
			window.Close();
			done(items.ToList());
		}, primary: true);
		button.IsDefault = true;
		window.Trailing(button);
		return window;
		void Build()
		{
			list.Children.Clear();
			for (int i = 0; i < items.Count; i++)
			{
				int i2 = i;
				Grid grid = new Grid
				{
					Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
				};
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(24.0)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(46.0)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				string text = L10n.T($"{i + 1}");
				Brush muted2 = Theme.Muted;
				TextBlock element = UI.Text(text, 12.0, null, muted2);
				grid.Children.Add(element);
				Image element2 = new Image
				{
					Source = thumbnails[items[i2]],
					Width = 34.0,
					Height = 26.0,
					Stretch = Stretch.Uniform
				};
				Grid.SetColumn(element2, 1);
				grid.Children.Add(element2);
				TextBlock textBlock = UI.Text(Path.GetFileName(items[i2]), 12.0);
				textBlock.Text = Path.GetFileName(items[i2]);
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
				Grid.SetColumn(textBlock, 2);
				grid.Children.Add(textBlock);
				StackPanel element3 = new StackPanel
				{
					Children = 
					{
						(UIElement)Arrow("\ue70e", i2 > 0, -1),
						(UIElement)Arrow("\ue70d", i2 < items.Count - 1, 1)
					}
				};
				Grid.SetColumn(element3, 3);
				grid.Children.Add(element3);
				list.Children.Add(new Border
				{
					Background = Theme.Card,
					CornerRadius = new CornerRadius(9.0),
					Padding = new Thickness(12.0, 8.0, 12.0, 8.0),
					Child = grid,
					Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
				});
				Button Arrow(string glyph, bool enabled, int by)
				{
					Button button2 = UI.IconButton(glyph, delegate
					{
						List<string> list2 = items;
						int index = i2;
						List<string> list3 = items;
						int index2 = i2 + by;
						string value = items[i2 + by];
						string value2 = items[i2];
						list2[index] = value;
						list3[index2] = value2;
						Build();
					}, (by < 0) ? "Move up" : "Move down", 18.0);
					button2.Width = 32.0;
					button2.Height = 15.0;
					button2.Margin = new Thickness(0.0, 1.0, 0.0, 1.0);
					button2.IsEnabled = enabled;
					((TextBlock)button2.Content).FontSize = 8.0;
					return button2;
				}
			}
		}
	}
}
