using System;
using System.Collections.Generic;
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
using ImageMagick;
using Microsoft.Win32;

namespace ShortTech.App.Windows;

public static class FrameWindow
{
	public static Window Make(IReadOnlyList<string> files)
	{
		FrameOptions options = new FrameOptions();
		(MagickImage, int, int) tuple = Pictures.LoadPreview(files[0], 900);
		MagickImage preview = tuple.Item1;
		int item = tuple.Item2;
		double scale = (double)preview.Width / (double)item;
		ToolWindow window = new ToolWindow("Add Background", 600.0);
		PictureStage stage = new PictureStage(340.0)
		{
			Background = new SolidColorBrush(Color.FromArgb(16, 0, 0, 0))
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(stage);
		DispatcherTimer pending = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(120L)
		};
		pending.Tick += async delegate
		{
			pending.Stop();
			FrameOptions current = options;
			try
			{
				BitmapSource source = await Task.Run(delegate
				{
					using MagickImage image = Frames.Render(preview, current, scale);
					return Pictures.ToBitmap(image);
				});
				stage.Source = source;
			}
			catch (ShortTechException ex)
			{
				Hud.Shared.ShowMessage(ex.Message, Hud.Style.Failure);
			}
		};
		WrapPanel swatches = new WrapPanel
		{
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		for (int num = 0; num < FrameBackground.Gradients.Length; num++)
		{
			Rgb[] array = FrameBackground.Gradients[num];
			Swatch(new LinearGradientBrush(Color.FromRgb((byte)(array[0].R * 255.0), (byte)(array[0].G * 255.0), (byte)(array[0].B * 255.0)), Color.FromRgb((byte)(array[1].R * 255.0), (byte)(array[1].G * 255.0), (byte)(array[1].B * 255.0)), 45.0), new FrameBackground.Gradient(num));
		}
		Rgb[] solids = FrameBackground.Solids;
		for (int num2 = 0; num2 < solids.Length; num2++)
		{
			Rgb color = solids[num2];
			Swatch(new SolidColorBrush(Color.FromRgb((byte)(color.R * 255.0), (byte)(color.G * 255.0), (byte)(color.B * 255.0))), new FrameBackground.Solid(color));
		}
		Swatch(Brushes.White, new FrameBackground.Blurred(), "\ue727");
		Swatch(Brushes.White, new FrameBackground.Picture(""), "\ueb9f");
		stackPanel.Children.Add(UI.Row("Background", swatches));
		stackPanel.Children.Add(UI.Row("Padding", UI.Slider(0.0, 600.0, options.Padding, delegate(double v)
		{
			options = options with
			{
				Padding = v
			};
			Refresh();
		}, (double v) => $"{Math.Round(v)} px")));
		stackPanel.Children.Add(UI.Row("Corners", UI.Slider(0.0, 200.0, options.Corners, delegate(double v)
		{
			options = options with
			{
				Corners = v
			};
			Refresh();
		}, (double v) => $"{Math.Round(v)} px")));
		stackPanel.Children.Add(UI.Row("Shadow", UI.Slider(0.0, 120.0, options.Shadow, delegate(double v)
		{
			options = options with
			{
				Shadow = v
			};
			Refresh();
		}, (double v) => $"{Math.Round(v)} px")));
		stackPanel.Children.Add(UI.Row("Ratio", UI.Segmented((from r in Enum.GetValues<FrameRatio>()
			select (r: r, r.Title())).ToList(), FrameRatio.Auto, delegate(FrameRatio r)
		{
			options = options with
			{
				Ratio = r
			};
			Refresh();
		})));
		if (files.Count > 1)
		{
			stackPanel.Children.Add(UI.Caption($"The same background is added to all {files.Count} images."));
		}
		window.Body = stackPanel;
		window.Leading(UI.Button("Reset", delegate
		{
			window.Close();
			ToolWindows.Open("frame", files);
		}));
		window.Trailing(UI.Button("Save with Background", delegate
		{
			FrameOptions chosen = options;
			window.Close();
			Runner.Run("Add Background", files, (IReadOnlyList<string> f, OperationContext c) => Frames.Apply(f, chosen, c));
		}, primary: true));
		Refresh();
		return window;
		void Refresh()
		{
			pending.Stop();
			pending.Start();
		}
		void Swatch(Brush fill, FrameBackground background, string? glyph = null)
		{
			Border border = new Border
			{
				Width = 26.0,
				Height = 26.0,
				CornerRadius = new CornerRadius(13.0),
				Background = fill,
				Margin = new Thickness(3.0),
				BorderBrush = Theme.Hairline,
				BorderThickness = new Thickness(1.0),
				Cursor = Cursors.Hand
			};
			if (glyph != null)
			{
				border.Child = new TextBlock
				{
					Text = glyph,
					FontFamily = Theme.Icons,
					FontSize = 12.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Theme.Ink
				};
			}
			border.MouseLeftButtonUp += delegate
			{
				if (background is FrameBackground.Picture)
				{
					OpenFileDialog openFileDialog = new OpenFileDialog
					{
						Filter = "Images|*.jpg;*.jpeg;*.png;*.heic;*.webp;*.avif;*.tif;*.tiff;*.bmp;*.gif",
						Title = L10n.T("Choose a background image")
					};
					if (openFileDialog.ShowDialog() != true)
					{
						return;
					}
					background = new FrameBackground.Picture(openFileDialog.FileName);
				}
				options = options with
				{
					Background = background
				};
				Refresh();
			};
			swatches.Children.Add(border);
		}
	}
}
