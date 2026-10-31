using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Images;

namespace ShortTech.App.Windows;

public static class ResizeWindow
{
	private static readonly int[] Presets = new int[4] { 800, 1280, 1920, 3840 };

	private const int Custom = 0;

	private static readonly Brush Problem = new SolidColorBrush(Color.FromRgb(196, 43, 28));

	public static Window Make(IReadOnlyList<string> files)
	{
		Settings app = Settings.Current;
		int saved = (app.ResizeMaxPixels > 0) ? app.ResizeMaxPixels : 1920;
		ToolWindow window = new ToolWindow("Resize Images", 440.0);
		StackPanel stack = new StackPanel();
		TextBox box = new TextBox
		{
			Text = saved.ToString(CultureInfo.InvariantCulture),
			Width = 80.0,
			Height = 26.0,
			TextAlignment = TextAlignment.Right,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		StackPanel field = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		field.Children.Add(box);
		field.Children.Add(UI.Text("px", 12.0, null, Theme.Muted).Pad(6.0, 0.0));
		Grid customRow = UI.Row("Custom size", field);
		List<(int, string)> options = Presets.Select((int p) => (p, $"{p} px")).ToList();
		options.Add((Custom, "Custom"));
		bool isPreset = Presets.Contains(saved);
		customRow.Visibility = isPreset ? Visibility.Collapsed : Visibility.Visible;
		stack.Children.Add(UI.Text("Longest side"));
		stack.Children.Add(UI.Segmented(options, isPreset ? saved : Custom, delegate(int choice)
		{
			customRow.Visibility = (choice == Custom) ? Visibility.Visible : Visibility.Collapsed;
			if (choice != Custom)
			{
				box.Text = choice.ToString(CultureInfo.InvariantCulture);
			}
		}).Pad(0.0, 6.0));
		stack.Children.Add(customRow);
		stack.Children.Add(UI.Caption("Images keep their proportions. Images already smaller than this keep their size.").Pad(0.0, 4.0));
		TextBlock message = UI.Text("", 12.0, null, Problem, wrap: true);
		stack.Children.Add(message);
		window.Body = stack;
		Button resize = UI.Button("Resize", delegate
		{
			if (!TryRead(box.Text, out var size))
			{
				return;
			}
			app.ResizeMaxPixels = size;
			app.Save();
			window.Close();
			Runner.Run("Resize Images", files, (IReadOnlyList<string> f, OperationContext c) => ImageOps.Resize(f, size, c));
		}, primary: true);
		resize.IsDefault = true;
		box.TextChanged += delegate
		{
			bool valid = TryRead(box.Text, out _);
			resize.IsEnabled = valid;
			message.Text = valid ? "" : L10n.T("Enter a size between 16 and 20000 pixels.");
		};
		window.Trailing(resize);
		return window;
	}

	private static bool TryRead(string text, out int size)
	{
		return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out size) && size >= 16 && size <= 20000;
	}
}
