using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;

namespace ShortTech.App.Windows;

public static class CompressWindow
{
	private static readonly int[] ImageSizes = new int[9] { 3840, 2560, 2048, 1920, 1600, 1280, 1024, 800, 640 };

	private static readonly int[] VideoHeights = new int[5] { 2160, 1440, 1080, 720, 480 };

	public static Window? Make(IReadOnlyList<string> files)
	{
		FileKind? kind = files.CommonKind();
		if (!kind.HasValue)
		{
			goto IL_0048;
		}
		switch (kind.GetValueOrDefault())
		{
		case FileKind.Image:
		case FileKind.Video:
		case FileKind.Audio:
			break;
		default:
			goto IL_0048;
		}
		bool flag = true;
		goto IL_004b;
		IL_004b:
		if (!flag)
		{
			return null;
		}
		Settings app = Settings.Current;
		CompressPreset preset = app.CompressPreset;
		int imageMax = app.CompressImageMaxPixels;
		int videoMax = app.CompressVideoMaxHeight;
		bool useTarget = false;
		bool bySize = true;
		long original = files.Sum((string f) => new FileInfo(f).Length);
		double num = (double)(original / Math.Max(1, files.Count)) / 2.0;
		string unit = ((num >= 1000000000.0) ? "GB" : ((num >= 1000000.0) ? "MB" : "KB"));
		double targetValue = Math.Round(num / UnitBytes(unit) * 10.0) / 10.0;
		double reduction = 50.0;
		ToolWindow window = new ToolWindow("Compress", 400.0);
		StackPanel stackPanel = new StackPanel();
		string text = ((files.Count == 1) ? Path.GetFileName(files[0]) : $"{files.Count} files");
		Brush muted = Theme.Muted;
		TextBlock textBlock = UI.Text(text, 12.0, null, muted);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		stackPanel.Children.Add(textBlock);
		stackPanel.Children.Add(UI.Text("Original size: " + Format.Bytes(original), 14.0, FontWeights.SemiBold).Pad(0.0, 2.0));
		StackPanel presetBlock = new StackPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		presetBlock.Children.Add(UI.Text("Compression preset", 14.0, FontWeights.SemiBold));
		TextBlock detail = UI.Caption(preset.Detail());
		TextBlock estimateText = UI.Text("—", 13.0, FontWeights.SemiBold);
		muted = Theme.Muted;
		TextBlock savedText = UI.Text("", 12.0, null, muted);
		muted = Theme.Accent;
		TextBlock message = UI.Text("", 11.5, null, muted, wrap: true);
		Button compressButton = UI.Button("Compress", delegate
		{
		}, primary: true);
		CancellationTokenSource pending = null;
		Border border = UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(CompressPreset, string)>(new(CompressPreset, string)[3]
		{
			(CompressPreset.Light, "Light"),
			(CompressPreset.Balanced, "Balanced"),
			(CompressPreset.Strong, "Strong")
		}), preset, delegate(CompressPreset p)
		{
			preset = p;
			detail.Text = L10n.T(p.Detail());
			Refresh();
		});
		border.HorizontalAlignment = HorizontalAlignment.Left;
		border.Margin = new Thickness(0.0, 6.0, 0.0, 4.0);
		presetBlock.Children.Add(border);
		presetBlock.Children.Add(detail);
		stackPanel.Children.Add(presetBlock);
		if (kind == FileKind.Image)
		{
			List<(int, string)> list = new List<(int, string)> { (0, "Keep original dimensions") };
			list.AddRange(ImageSizes.Select((int s) => (s: s, $"{s} px longest edge")));
			stackPanel.Children.Add(UI.Row("Image size", UI.Picker(list, imageMax, delegate(int v)
			{
				imageMax = v;
				Refresh();
			})).Pad(0.0, 8.0));
		}
		else if (kind == FileKind.Video)
		{
			List<(int, string)> list2 = new List<(int, string)> { (0, "Keep original") };
			list2.AddRange(VideoHeights.Select((int h) => (h: h, $"{h}p")));
			stackPanel.Children.Add(UI.Row("Maximum video height", UI.Picker(list2, videoMax, delegate(int v)
			{
				videoMax = v;
				Refresh();
			})).Pad(0.0, 8.0));
		}
		stackPanel.Children.Add(UI.Divider());
		StackPanel targetPanel = new StackPanel
		{
			Margin = new Thickness(20.0, 6.0, 0.0, 0.0),
			Visibility = Visibility.Collapsed
		};
		stackPanel.Children.Add(UI.Check("Compress to target file size", value: false, delegate(bool v)
		{
			useTarget = v;
			targetPanel.Visibility = ((!v) ? Visibility.Collapsed : Visibility.Visible);
			presetBlock.Opacity = (v ? 0.45 : 1.0);
			presetBlock.IsEnabled = !v;
			Refresh();
		}));
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		TextBox valueBox = new TextBox
		{
			Text = targetValue.ToString(CultureInfo.CurrentCulture),
			Width = 80.0,
			TextAlignment = TextAlignment.Right,
			VerticalContentAlignment = VerticalAlignment.Center,
			Height = 26.0
		};
		valueBox.TextChanged += delegate
		{
			if (double.TryParse(valueBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var result))
			{
				targetValue = result;
				Refresh();
			}
		};
		stackPanel2.Children.Add(valueBox);
		stackPanel2.Children.Add(UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(string, string)>(new(string, string)[3]
		{
			("KB", "KB"),
			("MB", "MB"),
			("GB", "GB")
		}), unit, delegate(string u)
		{
			unit = u;
			Refresh();
		}).Pad(6.0, 0.0));
		Grid sizeLine = UI.Row((files.Count > 1) ? "Size of each file" : "Target file size", stackPanel2);
		Grid reduceLine = UI.Row("Reduce by", UI.Slider(1.0, 99.0, reduction, delegate(double v)
		{
			reduction = Math.Round(v);
			Refresh();
		}, (double v) => L10n.T($"{Math.Round(v)}%"), null, 160.0, 1.0));
		reduceLine.Visibility = Visibility.Collapsed;
		targetPanel.Children.Add(UI.Row("Compression mode", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(true, "Target file size"),
			(false, "File size reduction")
		}), selected: true, delegate(bool v)
		{
			bySize = v;
			sizeLine.Visibility = ((!v) ? Visibility.Collapsed : Visibility.Visible);
			reduceLine.Visibility = (v ? Visibility.Collapsed : Visibility.Visible);
			Refresh();
		})));
		targetPanel.Children.Add(sizeLine);
		targetPanel.Children.Add(reduceLine);
		stackPanel.Children.Add(targetPanel);
		stackPanel.Children.Add(message.Pad(0.0, 6.0));
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		UIElementCollection children = stackPanel3.Children;
		muted = Theme.Muted;
		children.Add(UI.Text("Expected size:", 12.0, null, muted));
		stackPanel3.Children.Add(estimateText.Pad(6.0, 0.0));
		stackPanel3.Children.Add(savedText);
		stackPanel.Children.Add(stackPanel3);
		window.Body = stackPanel;
		window.Leading(UI.Button("Restore Defaults", delegate
		{
			window.Close();
			app.CompressPreset = CompressPreset.Balanced;
			app.CompressImageMaxPixels = 0;
			app.CompressVideoMaxHeight = 0;
			app.Save();
			ToolWindows.Open("compress", files);
		}));
		compressButton.Click += delegate
		{
			app.CompressPreset = preset;
			if (kind == FileKind.Image)
			{
				app.CompressImageMaxPixels = imageMax;
			}
			if (kind == FileKind.Video)
			{
				app.CompressVideoMaxHeight = videoMax;
			}
			app.Save();
			CompressSettings settings = Current();
			window.Close();
			Runner.Run("Compress", files, delegate(IReadOnlyList<string> f, OperationContext c)
			{
				if (kind.HasValue)
				{
					switch (kind.GetValueOrDefault())
					{
					case FileKind.Video:
						return Compression.Videos(f, settings, c);
					case FileKind.Audio:
						return Compression.Audio(f, settings, c);
					}
				}
				return Compression.Images(f, settings, c);
			});
		};
		window.Trailing(compressButton);
		Refresh();
		return window;
		IL_0048:
		flag = false;
		goto IL_004b;
		CompressSettings Current()
		{
			CompressTarget target = ((!useTarget) ? null : (bySize ? ((CompressTarget)new CompressTarget.Bytes((long)(targetValue * UnitBytes(unit)))) : ((CompressTarget)new CompressTarget.Reduction(reduction / 100.0))));
			return new CompressSettings(preset, (kind == FileKind.Image && imageMax > 0) ? new int?(imageMax) : ((int?)null), (kind == FileKind.Video && videoMax > 0) ? new int?(videoMax) : ((int?)null), target);
		}
		string? Problem()
		{
			if (!useTarget)
			{
				return null;
			}
			if (bySize)
			{
				double num2 = targetValue * UnitBytes(unit);
				if (num2 < 1000.0)
				{
					return "Enter a target size of at least 1 KB.";
				}
				if (files.Count == 1 && num2 >= (double)original)
				{
					return "Target size cannot exceed the original file size.";
				}
			}
			return null;
		}
		async void Refresh()
		{
			string text2 = Problem();
			message.Text = ((text2 != null) ? L10n.T(text2) : "");
			message.Visibility = ((text2 == null) ? Visibility.Collapsed : Visibility.Visible);
			compressButton.IsEnabled = text2 == null;
			pending?.Cancel();
			CancellationTokenSource cts = (pending = new CancellationTokenSource());
			estimateText.Text = "…";
			savedText.Text = "";
			try
			{
				await Task.Delay(250, cts.Token);
			}
			catch (TaskCanceledException)
			{
				return;
			}
			long? num2 = await Compression.Estimate(files, kind.Value, Current());
			if (!cts.IsCancellationRequested)
			{
				TextBlock textBlock2 = estimateText;
				object text3;
				if (num2.HasValue)
				{
					long valueOrDefault = num2.GetValueOrDefault();
					text3 = Format.Bytes(valueOrDefault);
				}
				else
				{
					text3 = "—";
				}
				textBlock2.Text = (string)text3;
				if (num2.HasValue)
				{
					long valueOrDefault2 = num2.GetValueOrDefault();
					if (original > 0)
					{
						int num3 = 100 - (int)Math.Round((double)valueOrDefault2 / (double)original * 100.0);
						savedText.Text = L10n.T((num3 > 0) ? $"about {num3}% smaller" : "little to gain");
					}
				}
			}
		}
		static double UnitBytes(string u)
		{
			if (u == "GB")
			{
				return 1000000000.0;
			}
			if (u == "MB")
			{
				return 1000000.0;
			}
			return 1000.0;
		}
	}
}
