using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ShortTech.Core;

namespace ShortTech.App;

public static class UI
{
	private static readonly Dictionary<(bool, bool), Style> Styles = new Dictionary<(bool, bool), Style>();

	public static TextBlock Text(string text, double size = 13.0, FontWeight? weight = null, Brush? color = null, bool wrap = false)
	{
		return new TextBlock
		{
			Text = L10n.T(text),
			FontFamily = Theme.Font,
			FontSize = size,
			FontWeight = (weight ?? FontWeights.Normal),
			Foreground = (color ?? Theme.Ink),
			TextWrapping = ((!wrap) ? TextWrapping.NoWrap : TextWrapping.Wrap),
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	public static TextBlock Caption(string text)
	{
		Brush muted = Theme.Muted;
		return Text(text, 11.5, null, muted, wrap: true);
	}

	public static TextBlock Icon(string glyph, double size = 13.0, Brush? color = null)
	{
		return new TextBlock
		{
			Text = glyph,
			FontFamily = Theme.Icons,
			FontSize = size,
			Foreground = (color ?? Theme.Ink),
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	public static Button Button(string title, Action click, bool primary = false, string? icon = null)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		if (icon != null)
		{
			stackPanel.Children.Add(Icon(icon, 12.0, primary ? Brushes.White : Theme.Ink));
			stackPanel.Children.Add(new Border
			{
				Width = 6.0
			});
		}
		stackPanel.Children.Add(Text(title, 13.0, FontWeights.Medium, primary ? Brushes.White : Theme.Ink));
		Button button = new Button();
		button.Content = stackPanel;
		button.Style = ButtonStyle(primary);
		button.Cursor = Cursors.Hand;
		button.Click += delegate
		{
			click();
		};
		return button;
	}

	public static Button IconButton(string glyph, Action click, string help, double size = 26.0)
	{
		Button button = new Button();
		button.Content = Icon(glyph, 11.0);
		button.Width = size;
		button.Height = size;
		button.Style = ButtonStyle(primary: false, round: true);
		button.ToolTip = L10n.T(help);
		button.Cursor = Cursors.Hand;
		button.Click += delegate
		{
			click();
		};
		return button;
	}

	private static Style ButtonStyle(bool primary, bool round = false)
	{
		if (Styles.TryGetValue((primary, round), out Style value))
		{
			return value;
		}
		ControlTemplate controlTemplate = new ControlTemplate(typeof(Button));
		FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(Border), "Back");
		frameworkElementFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(round ? 13 : 7));
		frameworkElementFactory.SetValue(Border.BackgroundProperty, primary ? Theme.Accent : Theme.ButtonFill);
		frameworkElementFactory.SetValue(Border.PaddingProperty, round ? new Thickness(0.0) : new Thickness(12.0, 4.0, 12.0, 4.0));
		frameworkElementFactory.SetValue(FrameworkElement.MinHeightProperty, 26.0);
		FrameworkElementFactory frameworkElementFactory2 = new FrameworkElementFactory(typeof(ContentPresenter));
		frameworkElementFactory2.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		frameworkElementFactory2.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		frameworkElementFactory.AppendChild(frameworkElementFactory2);
		controlTemplate.VisualTree = frameworkElementFactory;
		Trigger trigger = new Trigger
		{
			Property = ButtonBase.IsPressedProperty,
			Value = true
		};
		trigger.Setters.Add(new Setter(Border.BackgroundProperty, primary ? new SolidColorBrush(Color.FromRgb(72, 54, 196)) : Theme.ButtonPressed, "Back"));
		Trigger trigger2 = new Trigger
		{
			Property = UIElement.IsMouseOverProperty,
			Value = true
		};
		trigger2.Setters.Add(new Setter(Border.BackgroundProperty, primary ? new SolidColorBrush(Color.FromRgb(82, 63, 216)) : new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)), "Back"));
		Trigger trigger3 = new Trigger
		{
			Property = UIElement.IsEnabledProperty,
			Value = false
		};
		trigger3.Setters.Add(new Setter(UIElement.OpacityProperty, 0.4));
		controlTemplate.Triggers.Add(trigger2);
		controlTemplate.Triggers.Add(trigger);
		controlTemplate.Triggers.Add(trigger3);
		Style style = new Style(typeof(Button));
		style.Setters.Add(new Setter(Control.TemplateProperty, controlTemplate));
		style.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
		Styles[(primary, round)] = style;
		return style;
	}

	public static Grid Row(string title, UIElement control, double minHeight = 30.0)
	{
		Grid obj = new Grid
		{
			MinHeight = minHeight,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};
		TextBlock element = Text(title);
		obj.Children.Add(element);
		Grid.SetColumn(control, 2);
		obj.Children.Add(control);
		return obj;
	}

	public static Border Segmented<T>(IReadOnlyList<(T Value, string Title)> options, T selected, Action<T> changed)
	{
		UniformGrid uniformGrid = new UniformGrid
		{
			Rows = 1,
			Columns = options.Count
		};
		List<Border> buttons = new List<Border>();
		for (int i = 0; i < options.Count; i++)
		{
			int i2 = i;
			Border border = new Border
			{
				CornerRadius = new CornerRadius(6.0),
				Padding = new Thickness(10.0, 4.0, 10.0, 4.0),
				Margin = new Thickness(2.0),
				Cursor = Cursors.Hand,
				Child = new TextBlock
				{
					Text = L10n.T(options[i].Title),
					FontFamily = Theme.Font,
					FontSize = 12.5,
					Foreground = Theme.Ink,
					HorizontalAlignment = HorizontalAlignment.Center
				}
			};
			border.MouseLeftButtonUp += delegate
			{
				Select(i2);
				changed(options[i2].Value);
			};
			buttons.Add(border);
			uniformGrid.Children.Add(border);
		}
		Select(Math.Max(0, options.Select<(T, string), T>(((T Value, string Title) o) => o.Value).ToList().IndexOf(selected)));
		return new Border
		{
			Background = Theme.ButtonFill,
			CornerRadius = new CornerRadius(8.0),
			Child = uniformGrid
		};
		void Select(int index)
		{
			for (int j = 0; j < buttons.Count; j++)
			{
				buttons[j].Background = ((j == index) ? Brushes.White : Brushes.Transparent);
				buttons[j].Effect = ((j == index) ? new DropShadowEffect
				{
					BlurRadius = 3.0,
					ShadowDepth = 1.0,
					Direction = 270.0,
					Opacity = 0.15
				} : null);
				((TextBlock)buttons[j].Child).FontWeight = ((j == index) ? FontWeights.SemiBold : FontWeights.Normal);
			}
		}
	}

	public static CheckBox Check(string title, bool value, Action<bool> changed)
	{
		CheckBox checkBox = new CheckBox();
		checkBox.Content = Text(title);
		checkBox.IsChecked = value;
		checkBox.VerticalContentAlignment = VerticalAlignment.Center;
		checkBox.VerticalAlignment = VerticalAlignment.Center;
		checkBox.FontFamily = Theme.Font;
		checkBox.Checked += delegate
		{
			changed(obj: true);
		};
		checkBox.Unchecked += delegate
		{
			changed(obj: false);
		};
		return checkBox;
	}

	public static StackPanel Slider(double min, double max, double value, Action<double> changed, Func<double, string>? format = null, double? reset = null, double width = 160.0, double step = 0.0)
	{
		Slider slider = new Slider
		{
			Minimum = min,
			Maximum = max,
			Value = value,
			Width = width,
			VerticalAlignment = VerticalAlignment.Center,
			IsMoveToPointEnabled = true
		};
		if (step > 0.0)
		{
			slider.TickFrequency = step;
			slider.IsSnapToTickEnabled = true;
		}
		string text = format?.Invoke(value) ?? Math.Round(value).ToString();
		Brush muted = Theme.Muted;
		TextBlock label = Text(text, 12.0, null, muted);
		label.MinWidth = 48.0;
		label.TextAlignment = TextAlignment.Right;
		slider.ValueChanged += delegate(object _, RoutedPropertyChangedEventArgs<double> e)
		{
			label.Text = format?.Invoke(e.NewValue) ?? Math.Round(e.NewValue).ToString();
			changed(e.NewValue);
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		stackPanel.Children.Add(slider);
		stackPanel.Children.Add(label);
		if (reset.HasValue)
		{
			double r = reset.GetValueOrDefault();
			stackPanel.Children.Add(IconButton("\ue72c", delegate
			{
				slider.Value = r;
			}, "Reset", 22.0));
		}
		return stackPanel;
	}

	public static ComboBox Picker<T>(IReadOnlyList<(T Value, string Title)> options, T selected, Action<T> changed)
	{
		ComboBox box = new ComboBox
		{
			FontFamily = Theme.Font,
			FontSize = 12.5,
			MinWidth = 120.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		foreach (var option in options)
		{
			box.Items.Add(L10n.T(option.Title));
		}
		box.SelectedIndex = Math.Max(0, options.Select<(T, string), T>(((T Value, string Title) o) => o.Value).ToList().IndexOf(selected));
		box.SelectionChanged += delegate
		{
			if (box.SelectedIndex >= 0)
			{
				changed(options[box.SelectedIndex].Value);
			}
		};
		return box;
	}

	public static Border Divider()
	{
		return new Border
		{
			Height = 1.0,
			Background = Theme.Hairline,
			Margin = new Thickness(0.0, 8.0, 0.0, 8.0)
		};
	}

	public static T Pad<T>(this T element, double all) where T : FrameworkElement
	{
		element.Margin = new Thickness(all);
		return element;
	}

	public static T Pad<T>(this T element, double horizontal, double vertical) where T : FrameworkElement
	{
		element.Margin = new Thickness(horizontal, vertical, horizontal, vertical);
		return element;
	}
}
