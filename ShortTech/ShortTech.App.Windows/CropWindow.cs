using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Images;
using ImageMagick;

namespace ShortTech.App.Windows;

public static class CropWindow
{
	public static readonly (double? Value, string Title)[] Ratios = new(double?, string)[6]
	{
		(null, "Free"),
		(1.0, "1:1"),
		(1.7777777777777777, "16:9"),
		(0.5625, "9:16"),
		(1.3333333333333333, "4:3"),
		(0.75, "3:4")
	};

	public static Window Make(IReadOnlyList<string> files)
	{
		(MagickImage, int, int) tuple = Pictures.LoadPreview(files[0]);
		MagickImage item = tuple.Item1;
		int width = tuple.Item2;
		int height = tuple.Item3;
		ToolWindow window = new ToolWindow("Crop Image", 620.0);
		CropEditor editor = new CropEditor(380.0)
		{
			Source = Pictures.ToBitmap(item),
			PixelWidth = width,
			PixelHeight = height
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(editor);
		TextBox w = new TextBox
		{
			Width = 70.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			TextAlignment = System.Windows.TextAlignment.Right
		};
		TextBox h = new TextBox
		{
			Width = 70.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			TextAlignment = System.Windows.TextAlignment.Right
		};
		bool updating = false;
		editor.Changed += ShowSize;
		w.LostFocus += delegate
		{
			Typed();
		};
		h.LostFocus += delegate
		{
			Typed();
		};
		ComboBox control = UI.Picker(Ratios.Select(((double? Value, string Title) r) => (Value: r.Value, Title: r.Title)).ToList(), null, editor.SetRatio);
		stackPanel.Children.Add(UI.Row("Aspect ratio", control).Pad(0.0, 10.0));
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			FlowDirection = FlowDirection.LeftToRight
		};
		UIElementCollection children = stackPanel2.Children;
		Brush muted = Theme.Muted;
		children.Add(UI.Text("W", 12.0, null, muted).Pad(4.0, 0.0));
		stackPanel2.Children.Add(w);
		UIElementCollection children2 = stackPanel2.Children;
		muted = Theme.Muted;
		children2.Add(UI.Text("H", 12.0, null, muted).Pad(8.0, 0.0));
		stackPanel2.Children.Add(h);
		UIElementCollection children3 = stackPanel2.Children;
		muted = Theme.Muted;
		children3.Add(UI.Text("px", 12.0, null, muted).Pad(6.0, 0.0));
		stackPanel.Children.Add(UI.Row("Size", stackPanel2));
		if (files.Count > 1)
		{
			stackPanel.Children.Add(UI.Caption($"Uses this crop, as the same share of each image, for all {files.Count} images."));
		}
		ShowSize();
		window.Body = stackPanel;
		window.Leading(UI.Button("Reset", delegate
		{
			editor.SetRatio(null);
			editor.SetCrop(new Rect(0.0, 0.0, 1.0, 1.0));
		}));
		window.Trailing(UI.Button("Apply", delegate
		{
			Rect crop = editor.Crop;
			int result;
			int result2;
			(int, int)? exact = ((files.Count == 1 && int.TryParse(w.Text, out result) && int.TryParse(h.Text, out result2)) ? new(int, int)?((result, result2)) : (((int, int)?)null));
			window.Close();
			Runner.Run("Crop", files, (IReadOnlyList<string> f, OperationContext c) => ImageOps.Crop(f, (X: crop.X, Y: crop.Y, W: crop.Width, H: crop.Height), c, exact));
		}, primary: true));
		return window;
		void ShowSize()
		{
			updating = true;
			w.Text = Math.Round(editor.Crop.Width * (double)width).ToString(CultureInfo.InvariantCulture);
			h.Text = Math.Round(editor.Crop.Height * (double)height).ToString(CultureInfo.InvariantCulture);
			updating = false;
		}
		void Typed()
		{
			if (!updating && int.TryParse(w.Text, out var result) && int.TryParse(h.Text, out var result2) && result > 0 && result2 > 0)
			{
				double num = Math.Min(1.0, (double)result / (double)width);
				double num2 = Math.Min(1.0, (double)result2 / (double)height);
				editor.SetCrop(new Rect(Math.Min(editor.Crop.X, 1.0 - num), Math.Min(editor.Crop.Y, 1.0 - num2), num, num2));
			}
		}
	}
}
