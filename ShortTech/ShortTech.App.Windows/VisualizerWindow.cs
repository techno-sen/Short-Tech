using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShortTech.Core;
using ShortTech.Core.Images;
using ShortTech.Core.Media;
using ImageMagick;
using Microsoft.Win32;

namespace ShortTech.App.Windows;

public static class VisualizerWindow
{
	private sealed class PreviewFrame(MediaSession media) : FrameworkElement()
	{
		public VisualizerShape Shape { get; set; }

		public Color Color { get; set; }

		public BitmapSource? Picture { get; set; }

		public bool Still { get; set; }

		protected override void OnRender(DrawingContext dc)
		{
			(int Width, int Height) tuple = Shape.Size();
			int item = tuple.Width;
			int item2 = tuple.Height;
			double num = Math.Min(base.ActualWidth / (double)item, base.ActualHeight / (double)item2);
			Size size = new Size((double)item * num, (double)item2 * num);
			Rect rect = new Rect((base.ActualWidth - size.Width) / 2.0, (base.ActualHeight - size.Height) / 2.0, size.Width, size.Height);
			dc.PushClip(new RectangleGeometry(rect, 12.0, 12.0));
			dc.DrawRectangle(Brushes.Black, null, rect);
			if (Still && Picture != null)
			{
				double num2 = Math.Min(rect.Width / (double)Picture.PixelWidth, rect.Height / (double)Picture.PixelHeight);
				Size size2 = new Size((double)Picture.PixelWidth * num2, (double)Picture.PixelHeight * num2);
				dc.DrawImage(Picture, new Rect(rect.X + (rect.Width - size2.Width) / 2.0, rect.Y + (rect.Height - size2.Height) / 2.0, size2.Width, size2.Height));
			}
			else if (!Still && media.Waveform.Length != 0)
			{
				double num3 = Math.Min(120.0, rect.Height * 0.6);
				Timeline.DrawWave(dc, media.Waveform, new Rect(rect.X + 18.0, rect.Y + (rect.Height - num3) / 2.0, rect.Width - 36.0, num3), new SolidColorBrush(Color));
			}
			dc.Pop();
		}
	}

	public static Window? Make(IReadOnlyList<string> files)
	{
		return MediaUI.Open(files, Build, 10, 90);
	}

	private static ToolWindow Build(MediaSession media)
	{
		bool still = false;
		string image = null;
		BitmapSource picture = null;
		VisualizerShape shape = VisualizerShape.Landscape;
		Color color = Theme.AccentColor;
		ToolWindow window = new ToolWindow("Audio Visualizer", 480.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MediaUI.AudioHost(media));
		PreviewFrame preview = new PreviewFrame(media)
		{
			Height = 300.0
		};
		stackPanel.Children.Add(preview);
		Button create = UI.Button("Create Visualization", delegate
		{
		}, primary: true);
		Button imageButton = UI.Button("Choose Image…", delegate
		{
		});
		Grid imageRow = UI.Row("Image", imageButton, 34.0);
		imageRow.Visibility = Visibility.Collapsed;
		Grid colorRow = UI.Row("Color", ColorWell.Make(color, delegate(Color c)
		{
			color = c;
			Update();
		}), 34.0);
		stackPanel.Children.Add(UI.Row("Style", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(false, "Waveform"),
			(true, "Still image")
		}), selected: false, delegate(bool v)
		{
			still = v;
			imageRow.Visibility = ((!v) ? Visibility.Collapsed : Visibility.Visible);
			colorRow.Visibility = (v ? Visibility.Collapsed : Visibility.Visible);
			Update();
		}), 36.0).Pad(0.0, 10.0));
		stackPanel.Children.Add(imageRow);
		stackPanel.Children.Add(colorRow);
		stackPanel.Children.Add(UI.Row("Shape", UI.Picker((from s in Enum.GetValues<VisualizerShape>()
			select (s: s, s.Title())).ToList(), shape, delegate(VisualizerShape s)
		{
			shape = s;
			Update();
		}), 36.0));
		imageButton.Click += delegate
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = L10n.T("Choose an image for the visualization."),
				Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.heic;*.bmp;*.gif;*.tif;*.tiff"
			};
			if (openFileDialog.ShowDialog(window) == true)
			{
				try
				{
					MagickImage item = Pictures.LoadPreview(openFileDialog.FileName, 800).Preview;
					using (item)
					{
						picture = Pictures.ToBitmap(item);
					}
					image = openFileDialog.FileName;
					UIElementCollection children = ((StackPanel)imageButton.Content).Children;
					((TextBlock)children[children.Count - 1]).Text = Path.GetFileName(image);
				}
				catch (Exception)
				{
					Hud.Shared.ShowMessage(L10n.T("Could not read " + Path.GetFileName(openFileDialog.FileName)), Hud.Style.Failure);
				}
				Update();
			}
		};
		media.StripLoaded += preview.InvalidateVisual;
		window.Body = stackPanel;
		create.Click += delegate
		{
			string text = (still ? image : null);
			VisualizerShape visualizerShape = shape;
			Rgb rgb = MediaUI.Rgb(color);
			Rgb rgb2 = rgb;
			VisualizerShape s = visualizerShape;
			string file = text;
			window.Close();
			Runner.Run("Create Visualization", new global::_003C_003Ez__ReadOnlySingleElementList<string>(media.File), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await MediaTools.Visualize(f[0], file, s, rgb2, c)));
		};
		create.IsDefault = true;
		window.Trailing(create);
		Update();
		return window;
		void Update()
		{
			preview.Shape = shape;
			preview.Color = color;
			preview.Picture = (still ? picture : null);
			preview.Still = still;
			preview.InvalidateVisual();
			create.IsEnabled = !still || image != null;
		}
	}
}
