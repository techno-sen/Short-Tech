using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using ShortTech.Core;

namespace ShortTech.App;

public static class SettingsWindow
{
	private const string Website = "https://www.techno-sen.com";

	private const string LicenseWebPage = "https://www.gnu.org/licenses/gpl-3.0.html";

	private static ToolWindow? _open;

	private static bool _showingAbout;

	public static void Present()
	{
		Show(about: false);
	}

	public static void PresentAbout()
	{
		Show(about: true);
	}

	private static void Show(bool about)
	{
		if (_open != null)
		{
			if (_showingAbout == about)
			{
				_open.Activate();
				return;
			}
			_open.Close();
		}
		_open = Make(about);
		_open.Closed += delegate
		{
			_open = null;
		};
		_open.Present();
	}

	public static ToolWindow Make(bool about = false)
	{
		Settings settings = Settings.Current;
		ToolWindow window = new ToolWindow("Short Tech", 460.0);
		StackPanel stack = new StackPanel();
		(ModifierKey, string)[] options = new(ModifierKey, string)[4]
		{
			(ModifierKey.Shift, "Shift"),
			(ModifierKey.Ctrl, "Ctrl"),
			(ModifierKey.Alt, "Alt"),
			(ModifierKey.None, "None")
		};
		Section("Shortcuts while dragging");
		stack.Children.Add(UI.Row("Convert wheel", UI.Picker(options, settings.ConvertKey, delegate(ModifierKey k)
		{
			settings.ConvertKey = k;
			Save();
		})));
		stack.Children.Add(UI.Row("Tools wheel", UI.Picker(options, settings.ToolsKey, delegate(ModifierKey k)
		{
			settings.ToolsKey = k;
			Save();
		})));
		stack.Children.Add(UI.Row("Actions wheel", UI.Picker(options, settings.ActionsKey, delegate(ModifierKey k)
		{
			settings.ActionsKey = k;
			Save();
		})));
		stack.Children.Add(UI.Caption("Drag files anywhere, then press the key to open its wheel right away."));
		stack.Children.Add(UI.Divider());
		Section("Results");
		stack.Children.Add(UI.Row("Save results", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(OutputLocation, string)>(new(OutputLocation, string)[3]
		{
			(OutputLocation.NextToOriginal, "Next to the original"),
			(OutputLocation.Downloads, "Downloads folder"),
			(OutputLocation.Desktop, "Desktop")
		}), settings.OutputLocation, delegate(OutputLocation v)
		{
			settings.OutputLocation = v;
			Save();
		})));
		stack.Children.Add(UI.Check("Show results in a Float", settings.ShowResultsInFloat, delegate(bool v)
		{
			settings.ShowResultsInFloat = v;
			Save();
		}).Pad(0.0, 4.0));
		stack.Children.Add(UI.Check("Sound and haptic feedback", settings.PlaySounds, delegate(bool v)
		{
			settings.PlaySounds = v;
			Save();
		}).Pad(0.0, 4.0));
		stack.Children.Add(UI.Divider());
		Section("Compression");
		stack.Children.Add(UI.Row("Default preset", UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(CompressPreset, string)>(new(CompressPreset, string)[3]
		{
			(CompressPreset.Light, "Light"),
			(CompressPreset.Balanced, "Balanced"),
			(CompressPreset.Strong, "Strong")
		}), settings.CompressPreset, delegate(CompressPreset v)
		{
			settings.CompressPreset = v;
			Save();
		})));
		stack.Children.Add(UI.Row("Video", UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(VideoQuality, string)>(new(VideoQuality, string)[3]
		{
			(VideoQuality.High, "High quality"),
			(VideoQuality.Balanced, "Balanced"),
			(VideoQuality.Small, "Smallest file")
		}), settings.VideoQuality, delegate(VideoQuality v)
		{
			settings.VideoQuality = v;
			Save();
		})));
		stack.Children.Add(UI.Divider());
		Section("General");
		stack.Children.Add(UI.Check("Start Short Tech when Windows starts", Startup.IsEnabled, Startup.Set).Pad(0.0, 4.0));
		Button restart = UI.Button("Restart Now", delegate
		{
			Process.Start(new ProcessStartInfo(Environment.ProcessPath)
			{
				UseShellExecute = true
			});
			Application.Current.Shutdown();
		});
		restart.Visibility = Visibility.Collapsed;
		stack.Children.Add(UI.Row("Language", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(string, string)>(new(string, string)[2]
		{
			("ar", "العربية"),
			("en", "English")
		}), settings.Language, delegate(string v)
		{
			settings.Language = v;
			Save();
			restart.Visibility = ((!(v != (L10n.IsArabic ? "ar" : "en"))) ? Visibility.Collapsed : Visibility.Visible);
		})));
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		TextBlock textBlock = UI.Caption("Short Tech restarts to change the language.");
		DockPanel.SetDock(restart, Dock.Right);
		dockPanel.Children.Add(restart);
		dockPanel.Children.Add(textBlock);
		textBlock.SetBinding(UIElement.VisibilityProperty, new Binding("Visibility")
		{
			Source = restart
		});
		stack.Children.Add(dockPanel);
		string text = ProcessRunner.Find("ffmpeg");
		stack.Children.Add(UI.Caption((text != null) ? ("ffmpeg: " + text) : "ffmpeg is not installed. Install it with: winget install ffmpeg").Pad(0.0, 8.0));
		Button restoreDefaults = UI.Button("Restore Defaults", delegate
		{
			Settings settings2 = new Settings();
			settings.ConvertKey = settings2.ConvertKey;
			settings.ToolsKey = settings2.ToolsKey;
			settings.ActionsKey = settings2.ActionsKey;
			settings.OutputLocation = settings2.OutputLocation;
			settings.ShowResultsInFloat = settings2.ShowResultsInFloat;
			settings.PlaySounds = settings2.PlaySounds;
			settings.CompressPreset = settings2.CompressPreset;
			settings.VideoQuality = settings2.VideoQuality;
			Save();
			window.Close();
			Present();
		});
		window.Leading(restoreDefaults);
		window.Trailing(UI.Button("Close", window.Close, primary: true));
		StackPanel aboutPanel = About();
		Border tabs = UI.Segmented(new(bool, string)[2]
		{
			(false, "Settings"),
			(true, "About")
		}, about, Switch);
		tabs.Margin = new Thickness(0.0, 0.0, 0.0, 14.0);
		StackPanel root = new StackPanel();
		root.Children.Add(tabs);
		root.Children.Add(stack);
		root.Children.Add(aboutPanel);
		window.Body = root;
		Switch(about);
		return window;
		void Save()
		{
			settings.Save();
		}
		void Switch(bool showAbout)
		{
			_showingAbout = showAbout;
			stack.Visibility = showAbout ? Visibility.Collapsed : Visibility.Visible;
			aboutPanel.Visibility = showAbout ? Visibility.Visible : Visibility.Collapsed;
			restoreDefaults.Visibility = stack.Visibility;
		}
		void Section(string title)
		{
			stack.Children.Add(UI.Text(title, 12.0, FontWeights.SemiBold, Theme.Muted).Pad(0.0, 6.0));
		}
	}

	private static StackPanel About()
	{
		StackPanel panel = new StackPanel
		{
			Margin = new Thickness(8.0, 4.0, 8.0, 4.0)
		};
		panel.Children.Add(new Image
		{
			Source = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon-256.png")),
			Width = 80.0,
			Height = 80.0,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		panel.Children.Add(Centered(UI.Text("Short Tech", 22.0, FontWeights.Bold), 10.0));
		Version version = typeof(SettingsWindow).Assembly.GetName().Version ?? new Version(1, 0, 0);
		panel.Children.Add(Centered(UI.Text($"Version {version.Major}.{version.Minor}.{version.Build}", 12.0, null, Theme.Muted), 2.0));
		panel.Children.Add(Centered(UI.Text("Short Tech puts your everyday file tools one gesture away. Drag any file, press Shift, Ctrl or Alt, and pick what you need: convert, compress, edit, rename or protect, without opening another app.", 13.0, null, null, wrap: true), 16.0));
		panel.Children.Add(Centered(UI.Caption("Your files are processed on this computer. Short Tech never uploads them."), 8.0));
		panel.Children.Add(UI.Divider().Pad(0.0, 14.0));
		panel.Children.Add(Centered(UI.Text("Developed by Techno Sen", 13.0, FontWeights.SemiBold), 0.0));
		panel.Children.Add(Centered(Link("www.techno-sen.com", Website, 13.0), 4.0));
		panel.Children.Add(Centered(UI.Caption("Short Tech is free software under the GNU General Public License v3 and comes with no warranty."), 18.0));
		string licenseFile = Path.Combine(AppContext.BaseDirectory, "LICENSE.txt");
		panel.Children.Add(Centered(Link(L10n.T("View license"), File.Exists(licenseFile) ? licenseFile : LicenseWebPage, 11.5), 4.0));
		panel.Children.Add(Centered(UI.Caption("© 2026 Techno Sen"), 6.0));
		return panel;
		static TextBlock Centered(TextBlock text, double top)
		{
			text.HorizontalAlignment = HorizontalAlignment.Center;
			text.TextAlignment = TextAlignment.Center;
			text.Margin = new Thickness(0.0, top, 0.0, 0.0);
			return text;
		}
	}

	private static TextBlock Link(string text, string target, double size)
	{
		Hyperlink hyperlink = new Hyperlink(new Run(text))
		{
			Foreground = Theme.Accent,
			Cursor = System.Windows.Input.Cursors.Hand
		};
		hyperlink.Click += delegate
		{
			try
			{
				Process.Start(new ProcessStartInfo(target)
				{
					UseShellExecute = true
				});
			}
			catch (Win32Exception ex)
			{
				Log.Write("could not open " + target + ": " + ex.Message);
			}
		};
		TextBlock textBlock = new TextBlock
		{
			FontFamily = Theme.Font,
			FontSize = size
		};
		textBlock.Inlines.Add(hyperlink);
		return textBlock;
	}
}
