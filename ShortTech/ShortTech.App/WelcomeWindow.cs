using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.Core;
using ImageMagick;
using ImageMagick.Drawing;

namespace ShortTech.App;

public static class WelcomeWindow
{
	private static ToolWindow? _open;

	public static void Present()
	{
		if (_open != null)
		{
			_open.Activate();
			return;
		}
		_open = Make(0);
		_open.Closed += delegate
		{
			_open = null;
			Settings.Current.HasSeenWelcome = true;
			Settings.Current.Save();
		};
		_open.Present();
	}

	public static ToolWindow Make(int startPage)
	{
		ToolWindow window = new ToolWindow("Welcome to Short Tech", 580.0);
		ContentControl host = new ContentControl
		{
			Height = 380.0
		};
		StackPanel dots = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center
		};
		Button back = UI.Button("Back", delegate
		{
		});
		Button next = UI.Button("Continue", delegate
		{
		}, primary: true);
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(40L)
		};
		List<(Border Cap, int Key)> keycaps = new List<(Border, int)>();
		timer.Tick += delegate
		{
			foreach (var item2 in keycaps)
			{
				Border item = item2.Cap;
				bool flag = Native.IsDown(item2.Key);
				item.Background = (flag ? Theme.Accent : Brushes.White);
				foreach (TextBlock item3 in ((StackPanel)item.Child).Children.OfType<TextBlock>())
				{
					item3.Foreground = (flag ? Brushes.White : Theme.Ink);
				}
			}
		};
		back.Click += delegate
		{
			startPage = Math.Max(0, startPage - 1);
			Show();
		};
		next.Click += delegate
		{
			if (startPage == 3)
			{
				window.Close();
			}
			else
			{
				startPage++;
				Show();
			}
		};
		window.Closed += delegate
		{
			timer.Stop();
		};
		window.Body = host;
		window.Leading(back);
		new DockPanel().LastChildFill = false;
		window.Trailing(next);
		window.Leading(dots);
		Show();
		return window;
		void Show()
		{
			keycaps.Clear();
			ContentControl contentControl = host;
			contentControl.Content = startPage switch
			{
				0 => Intro(), 
				1 => Practice(keycaps), 
				2 => Preferences(), 
				_ => Finish(), 
			};
			dots.Children.Clear();
			for (int i = 0; i < 4; i++)
			{
				dots.Children.Add(new Border
				{
					Width = 7.0,
					Height = 7.0,
					CornerRadius = new CornerRadius(4.0),
					Margin = new Thickness(3.0, 0.0, 3.0, 0.0),
					Background = ((i == startPage) ? Theme.Accent : Theme.Hairline)
				});
			}
			back.Visibility = ((startPage == 0) ? Visibility.Hidden : Visibility.Visible);
			((TextBlock)((StackPanel)next.Content).Children[0]).Text = L10n.T((startPage == 3) ? "Get Started" : "Continue");
			if (startPage == 1)
			{
				timer.Start();
			}
			else
			{
				timer.Stop();
			}
		}
	}

	private static StackPanel Centered(params UIElement[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		foreach (UIElement uIElement in children)
		{
			if (uIElement is FrameworkElement frameworkElement)
			{
				frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
			}
			stackPanel.Children.Add(uIElement);
		}
		return stackPanel;
	}

	private static UIElement Intro()
	{
		Image image = new Image
		{
			Source = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon-256.png")),
			Width = 112.0,
			Height = 112.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
		};
		TextBlock textBlock = UI.Text("Welcome to Short Tech", 26.0, FontWeights.Bold);
		textBlock.FontFamily = Theme.Display;
		Brush muted = Theme.Muted;
		TextBlock textBlock2 = UI.Text("Convert, compress and edit any file while you drag it. Everything stays on your PC.", 14.0, null, muted, wrap: true);
		textBlock2.TextAlignment = System.Windows.TextAlignment.Center;
		textBlock2.MaxWidth = 380.0;
		textBlock2.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		return Centered(image, textBlock, textBlock2);
	}

	private static UIElement Practice(List<(Border, int)> keycaps)
	{
		TextBlock textBlock = UI.Text("Try it now", 22.0, FontWeights.Bold);
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 22.0);
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		string file = PracticeFile();
		StackPanel tile = new StackPanel
		{
			Cursor = Cursors.Hand
		};
		tile.Children.Add(new Image
		{
			Source = (ShellThumbnail.For(file, 160) ?? new BitmapImage(new Uri(file))),
			Width = 92.0,
			Height = 92.0
		});
		UIElementCollection children = tile.Children;
		string? fileName = Path.GetFileName(file);
		Brush muted = Theme.Muted;
		children.Add(UI.Text(fileName, 11.0, null, muted).Pad(0.0, 4.0));
		tile.MouseMove += delegate(object _, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragDrop.DoDragDrop(tile, new DataObject(DataFormats.FileDrop, new string[1] { file }), DragDropEffects.Copy);
			}
		};
		StackPanel element = Centered(Badge("1"), UI.Text("Step 1. Drag the photo.", 13.0, FontWeights.SemiBold).Pad(0.0, 8.0), tile);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			FlowDirection = FlowDirection.LeftToRight
		};
		Settings current = Settings.Current;
		(ModifierKey, string)[] array = new(ModifierKey, string)[3]
		{
			(current.ConvertKey, "Convert"),
			(current.ToolsKey, "Tools"),
			(current.ActionsKey, "Actions")
		};
		for (int num = 0; num < array.Length; num++)
		{
			var (modifierKey, text) = array[num];
			if (modifierKey != ModifierKey.None)
			{
				StackPanel stackPanel2 = new StackPanel();
				UIElementCollection children2 = stackPanel2.Children;
				children2.Add(UI.Text(modifierKey switch
				{
					ModifierKey.Shift => "⇧", 
					ModifierKey.Ctrl => "Ctrl", 
					_ => "Alt", 
				}, 15.0, FontWeights.SemiBold).Pad(0.0, 0.0));
				((TextBlock)stackPanel2.Children[0]).HorizontalAlignment = HorizontalAlignment.Center;
				string text2 = Tray.Key(modifierKey);
				muted = Theme.Muted;
				TextBlock textBlock2 = UI.Text(text2, 10.5, null, muted);
				textBlock2.HorizontalAlignment = HorizontalAlignment.Center;
				stackPanel2.Children.Add(textBlock2);
				Border border = new Border
				{
					Width = 70.0,
					Height = 60.0,
					CornerRadius = new CornerRadius(10.0),
					Background = Brushes.White,
					BorderBrush = Theme.Hairline,
					BorderThickness = new Thickness(1.0),
					Child = stackPanel2,
					Margin = new Thickness(5.0, 0.0, 5.0, 0.0),
					Padding = new Thickness(0.0, 8.0, 0.0, 0.0)
				};
				StackPanel stackPanel3 = new StackPanel();
				stackPanel3.Children.Add(border);
				muted = Theme.Muted;
				TextBlock textBlock3 = UI.Text(text, 11.0, null, muted);
				textBlock3.HorizontalAlignment = HorizontalAlignment.Center;
				textBlock3.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
				stackPanel3.Children.Add(textBlock3);
				stackPanel.Children.Add(stackPanel3);
				Border item = border;
				keycaps.Add((item, modifierKey switch
				{
					ModifierKey.Shift => 16, 
					ModifierKey.Ctrl => 17, 
					_ => 18, 
				}));
			}
		}
		StackPanel element2 = Centered(Badge("2"), UI.Text("Step 2. While dragging, press a key.", 13.0, FontWeights.SemiBold).Pad(0.0, 8.0), stackPanel);
		Grid.SetColumn(element2, 1);
		grid.Children.Add(element);
		grid.Children.Add(element2);
		Border obj = new Border
		{
			Background = Theme.Card,
			CornerRadius = new CornerRadius(14.0),
			Padding = new Thickness(14.0, 6.0, 14.0, 6.0),
			Margin = new Thickness(0.0, 26.0, 0.0, 0.0)
		};
		muted = Theme.Muted;
		obj.Child = UI.Text("The wheel stays open until you drop.", 12.0, null, muted);
		Border border2 = obj;
		return Centered(textBlock, grid, border2);
	}

	private static Border Badge(string number)
	{
		return new Border
		{
			Width = 26.0,
			Height = 26.0,
			CornerRadius = new CornerRadius(13.0),
			Background = Theme.Accent,
			Child = new TextBlock
			{
				Text = number,
				Foreground = Brushes.White,
				FontWeight = FontWeights.Bold,
				FontFamily = Theme.Font,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}

	private static UIElement Preferences()
	{
		Settings settings = Settings.Current;
		TextBlock textBlock = UI.Text("Make it yours", 22.0, FontWeights.Bold);
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 18.0);
		StackPanel stackPanel = new StackPanel
		{
			Width = 420.0
		};
		Button restart = UI.Button("Restart Now", delegate
		{
			Process.Start(new ProcessStartInfo(Environment.ProcessPath)
			{
				UseShellExecute = true
			});
			Application.Current.Shutdown();
		});
		restart.Visibility = Visibility.Collapsed;
		stackPanel.Children.Add(UI.Row("Language", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(string, string)>(new(string, string)[2]
		{
			("ar", "العربية"),
			("en", "English")
		}), settings.Language, delegate(string v)
		{
			settings.Language = v;
			settings.Save();
			restart.Visibility = ((!(v != (L10n.IsArabic ? "ar" : "en"))) ? Visibility.Collapsed : Visibility.Visible);
		})));
		stackPanel.Children.Add(restart);
		stackPanel.Children.Add(UI.Divider());
		stackPanel.Children.Add(UI.Check("Start Short Tech when Windows starts", Startup.IsEnabled, Startup.Set).Pad(0.0, 4.0));
		stackPanel.Children.Add(UI.Caption("Short Tech lives next to the clock, so it is ready whenever you drag a file."));
		stackPanel.Children.Add(UI.Divider());
		stackPanel.Children.Add(UI.Check("Sound and haptic feedback", settings.PlaySounds, delegate(bool v)
		{
			settings.PlaySounds = v;
			settings.Save();
		}).Pad(0.0, 4.0));
		stackPanel.Children.Add(UI.Check("Show results in a Float", settings.ShowResultsInFloat, delegate(bool v)
		{
			settings.ShowResultsInFloat = v;
			settings.Save();
		}).Pad(0.0, 4.0));
		Border border = new Border
		{
			Background = Brushes.White,
			CornerRadius = new CornerRadius(14.0),
			Padding = new Thickness(18.0),
			BorderBrush = Theme.Hairline,
			BorderThickness = new Thickness(1.0),
			Child = stackPanel
		};
		return Centered(textBlock, border);
	}

	private static UIElement Finish()
	{
		Border border = new Border
		{
			Width = 72.0,
			Height = 72.0,
			CornerRadius = new CornerRadius(36.0),
			Background = Theme.AccentSoft,
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0),
			Child = UI.Icon("\ue73e", 30.0, Theme.Accent)
		};
		((TextBlock)border.Child).HorizontalAlignment = HorizontalAlignment.Center;
		TextBlock textBlock = UI.Text("You're all set", 24.0, FontWeights.Bold);
		Brush muted = Theme.Muted;
		TextBlock textBlock2 = UI.Text("Find the Short Tech icon next to the clock for Floats, Settings and this guide.", 14.0, null, muted, wrap: true);
		textBlock2.TextAlignment = System.Windows.TextAlignment.Center;
		textBlock2.MaxWidth = 380.0;
		textBlock2.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		return Centered(border, textBlock, textBlock2);
	}

	private static string PracticeFile()
	{
		string text = Path.Combine(Settings.Folder, "Practice");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, "Practice Photo.png");
		if (File.Exists(text2))
		{
			return text2;
		}
		MagickReadSettings magickReadSettings = new MagickReadSettings
		{
			Width = 600u,
			Height = 600u
		};
		magickReadSettings.SetDefine("gradient:angle", "135");
		using MagickImage magickImage = new MagickImage("gradient:#7F70FF-#3B2AA8", magickReadSettings);
		new Drawables().FillColor(new MagickColor("#7CF2C8")).Circle(300.0, 300.0, 300.0, 170.0).Draw(magickImage);
		magickImage.Write(text2, MagickFormat.Png);
		return text2;
	}
}
