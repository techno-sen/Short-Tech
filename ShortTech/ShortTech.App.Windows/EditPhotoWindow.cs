using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.Core;
using ShortTech.Core.Images;
using ImageMagick;

namespace ShortTech.App.Windows;

public static class EditPhotoWindow
{
	private sealed class BitmapSourceHolder
	{
		public BitmapSource? Bitmap;
	}

	public static Window Make(IReadOnlyList<string> files)
	{
		string file = files[0];
		(MagickImage, int, int) tuple = Pictures.LoadPreview(file, 1200);
		MagickImage preview = tuple.Item1;
		int item = tuple.Item2;
		double scale = (double)preview.Width / (double)item;
		PhotoAdjustments adjustments = new PhotoAdjustments();
		BitmapSource original = Pictures.ToBitmap(preview);
		ToolWindow window = new ToolWindow("Edit Photo", 680.0);
		PictureStage stage = new PictureStage(330.0)
		{
			Source = original
		};
		Border badge = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(204, 0, 0, 0)),
			CornerRadius = new CornerRadius(9.0),
			Padding = new Thickness(8.0, 2.0, 8.0, 2.0),
			Margin = new Thickness(10.0),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Child = UI.Text("Original", 11.0, FontWeights.SemiBold, Brushes.White)
		};
		stage.Children.Add(badge);
		BitmapSourceHolder edited = new BitmapSourceHolder
		{
			Bitmap = original
		};
		DispatcherTimer pending = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(90L)
		};
		bool busy = false;
		pending.Tick += async delegate
		{
			if (!busy)
			{
				pending.Stop();
				busy = true;
				PhotoAdjustments copy = adjustments.Copy();
				BitmapSourceHolder bitmapSourceHolder = edited;
				bitmapSourceHolder.Bitmap = await Task.Run(delegate
				{
					using MagickImage image = (MagickImage)preview.Clone();
					copy.Apply(image, scale);
					return Pictures.ToBitmap(image);
				});
				stage.Source = edited.Bitmap;
				((TextBlock)badge.Child).Text = L10n.T(copy.IsNeutral ? "Original" : "Edited");
				busy = false;
			}
		};
		stage.MouseLeftButtonDown += delegate
		{
			stage.Source = original;
			((TextBlock)badge.Child).Text = L10n.T("Original");
		};
		stage.MouseLeftButtonUp += delegate
		{
			stage.Source = edited.Bitmap;
			((TextBlock)badge.Child).Text = L10n.T(adjustments.IsNeutral ? "Original" : "Edited");
		};
		stage.ToolTip = L10n.T("Hold to compare with the original");
		StackPanel stackPanel = new StackPanel();
		List<Action> resets = new List<Action>();
		foreach (var (text, readOnlyList) in PhotoAdjustments.Sections)
		{
			stackPanel.Children.Add(UI.Text((text == "Light" && L10n.IsArabic) ? "الإضاءة" : text, 11.5, FontWeights.SemiBold, Theme.Muted).Pad(0.0, 6.0));
			foreach (AdjustmentInfo item2 in readOnlyList)
			{
				StackPanel control = UI.Slider(item2.Min, item2.Max, 0.0, delegate(double v)
				{
					adjustments[item2.Key] = ((item2.Key == Adjust.Exposure) ? Math.Round(v, 2) : Math.Round(v));
					pending.Stop();
					pending.Start();
				}, (double v) => (item2.Key != Adjust.Exposure) ? (Math.Round(v) + item2.Unit) : (v.ToString("0.00", CultureInfo.InvariantCulture) + item2.Unit), 0.0, 280.0);
				resets.Add(delegate
				{
					((Slider)control.Children[0]).Value = 0.0;
				});
				stackPanel.Children.Add(UI.Row(item2.Title, control, 28.0));
			}
		}
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Children.Add(stage);
		stackPanel2.Children.Add(new ScrollViewer
		{
			Content = stackPanel,
			Height = 250.0,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0),
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		});
		window.Body = stackPanel2;
		window.Leading(UI.Button("Reset", delegate
		{
			foreach (Action item3 in resets)
			{
				item3();
			}
		}));
		window.Trailing(UI.Button("Save Edited Copy", delegate
		{
			PhotoAdjustments chosen = adjustments.Copy();
			window.Close();
			Runner.Run("Edit Photo", new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await PhotoAdjustments.Save(f[0], chosen, c)));
		}, primary: true));
		return window;
	}
}
