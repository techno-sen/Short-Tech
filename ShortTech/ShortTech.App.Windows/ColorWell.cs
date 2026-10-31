using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ShortTech.Core;

namespace ShortTech.App.Windows;

public static class ColorWell
{
	private static readonly string[] Palette = new string[24]
	{
		"#000000", "#3A3A3C", "#8E8E93", "#C7C7CC", "#FFFFFF", "#5B47E6", "#1E3A8A", "#2563EB", "#0EA5E9", "#14B8A6",
		"#16A34A", "#84CC16", "#FACC15", "#F59E0B", "#EA580C", "#DC2626", "#DB2777", "#9333EA", "#7C2D12", "#365314",
		"#0F766E", "#312E81", "#701A75", "#881337"
	};

	public static Border Make(Color value, Action<Color> changed)
	{
		Border swatch = new Border
		{
			Width = 26.0,
			Height = 26.0,
			CornerRadius = new CornerRadius(13.0),
			Background = new SolidColorBrush(value),
			BorderBrush = Theme.Hairline,
			BorderThickness = new Thickness(1.5),
			Cursor = Cursors.Hand
		};
		Popup popup = new Popup
		{
			PlacementTarget = swatch,
			Placement = PlacementMode.Bottom,
			StaysOpen = false,
			AllowsTransparency = true
		};
		UniformGrid uniformGrid = new UniformGrid
		{
			Columns = 6
		};
		TextBox hex = new TextBox
		{
			Height = 24.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center,
			Text = QrDesign.Hex(value),
			FlowDirection = FlowDirection.LeftToRight
		};
		string[] palette = Palette;
		foreach (string value2 in palette)
		{
			Color color = (Color)ColorConverter.ConvertFromString(value2);
			Border border = new Border
			{
				Width = 22.0,
				Height = 22.0,
				Margin = new Thickness(3.0),
				CornerRadius = new CornerRadius(11.0),
				Background = new SolidColorBrush(color),
				BorderBrush = Theme.Hairline,
				BorderThickness = new Thickness(1.0),
				Cursor = Cursors.Hand
			};
			border.MouseLeftButtonUp += delegate
			{
				Set(color);
				popup.IsOpen = false;
			};
			uniformGrid.Children.Add(border);
		}
		hex.KeyDown += delegate(object _, KeyEventArgs e)
		{
			if (e.Key != Key.Return)
			{
				return;
			}
			try
			{
				Set((Color)ColorConverter.ConvertFromString(hex.Text.StartsWith('#') ? hex.Text : ("#" + hex.Text)));
				popup.IsOpen = false;
			}
			catch (FormatException)
			{
			}
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(uniformGrid);
		stackPanel.Children.Add(hex);
		popup.Child = new Border
		{
			Background = Brushes.White,
			CornerRadius = new CornerRadius(10.0),
			Padding = new Thickness(8.0),
			BorderBrush = Theme.Hairline,
			BorderThickness = new Thickness(1.0),
			Margin = new Thickness(6.0),
			Effect = new DropShadowEffect
			{
				BlurRadius = 12.0,
				ShadowDepth = 2.0,
				Direction = 270.0,
				Opacity = 0.2
			},
			Child = stackPanel
		};
		swatch.MouseLeftButtonUp += delegate
		{
			if (swatch.IsEnabled)
			{
				popup.IsOpen = true;
			}
		};
		return swatch;
		void Set(Color color2)
		{
			swatch.Background = new SolidColorBrush(color2);
			hex.Text = QrDesign.Hex(color2);
			changed(color2);
		}
	}
}
