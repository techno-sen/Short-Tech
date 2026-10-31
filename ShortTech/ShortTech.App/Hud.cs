using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using ShortTech.Core;

namespace ShortTech.App;

public sealed class Hud : Window
{
	public new enum Style
	{
		Working,
		Success,
		Failure
	}

	private readonly TextBlock _icon = new TextBlock
	{
		FontFamily = Theme.Icons,
		FontSize = 15.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly TextBlock _text = new TextBlock
	{
		FontFamily = Theme.Font,
		FontSize = 12.5,
		FontWeight = FontWeights.Medium,
		Foreground = Theme.Ink,
		TextWrapping = TextWrapping.Wrap,
		MaxWidth = 340.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly TextBlock _percent = new TextBlock
	{
		FontFamily = Theme.Font,
		FontSize = 11.0,
		FontWeight = FontWeights.SemiBold,
		Foreground = Theme.Muted,
		Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly ProgressBar _bar = new ProgressBar
	{
		Height = 4.0,
		Minimum = 0.0,
		Maximum = 1.0,
		Foreground = Theme.Accent,
		Background = Theme.ButtonFill,
		BorderThickness = new Thickness(0.0),
		Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
		Visibility = Visibility.Collapsed
	};

	private readonly DispatcherTimer _hideTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(2.6)
	};

	private Style _style;

	public static Hud Shared { get; } = new Hud();

	private Hud()
	{
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.Background = Brushes.Transparent;
		base.ShowInTaskbar = false;
		base.ShowActivated = false;
		base.Topmost = true;
		base.ResizeMode = ResizeMode.NoResize;
		base.SizeToContent = SizeToContent.WidthAndHeight;
		base.FlowDirection = Theme.Direction;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		stackPanel.Children.Add(_icon);
		stackPanel.Children.Add(new Border
		{
			Width = 9.0
		});
		stackPanel.Children.Add(_text);
		stackPanel.Children.Add(_percent);
		StackPanel child = new StackPanel
		{
			Children = 
			{
				(UIElement)stackPanel,
				(UIElement)_bar
			}
		};
		base.Content = new Border
		{
			Margin = new Thickness(18.0),
			Padding = new Thickness(14.0, 9.0, 14.0, 9.0),
			CornerRadius = new CornerRadius(12.0),
			Background = new SolidColorBrush(Color.FromArgb(245, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			BorderBrush = Theme.Hairline,
			BorderThickness = new Thickness(1.0),
			Effect = new DropShadowEffect
			{
				BlurRadius = 18.0,
				ShadowDepth = 4.0,
				Direction = 270.0,
				Opacity = 0.16
			},
			Child = child
		};
		_hideTimer.Tick += delegate
		{
			_hideTimer.Stop();
			Fade();
		};
		base.SourceInitialized += delegate
		{
			Native.MakeToolWindow(new WindowInteropHelper(this).Handle, clickThrough: true);
		};
		base.SizeChanged += delegate
		{
			Position();
		};
	}

	public void ShowMessage(string text, Style style)
	{
		_style = style;
		_text.Text = L10n.T(text);
		TextBlock icon = _icon;
		icon.Text = style switch
		{
			Style.Working => "\ue895", 
			Style.Success => "\ue930", 
			_ => "\ue783", 
		};
		_icon.Foreground = ((style == Style.Failure) ? new SolidColorBrush(Color.FromRgb(216, 59, 59)) : Theme.Accent);
		_bar.Visibility = Visibility.Collapsed;
		_percent.Text = "";
		if (style == Style.Working)
		{
			_hideTimer.Stop();
		}
		else
		{
			_hideTimer.Interval = TimeSpan.FromSeconds((style == Style.Failure) ? 4.0 : 2.6);
			_hideTimer.Start();
		}
		BeginAnimation(UIElement.OpacityProperty, null);
		base.Opacity = 1.0;
		if (!base.IsVisible)
		{
			Show();
		}
		Position();
	}

	public void Update(string text)
	{
		if (_style == Style.Working)
		{
			_text.Text = L10n.T(text);
		}
	}

	public void SetProgress(double fraction)
	{
		if (_style == Style.Working)
		{
			_bar.Visibility = Visibility.Visible;
			_bar.Value = fraction;
			_percent.Text = $"{(int)Math.Round(fraction * 100.0)}%";
		}
	}

	private void Fade()
	{
		DoubleAnimation doubleAnimation = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(220L));
		doubleAnimation.Completed += delegate
		{
			if (base.Opacity == 0.0)
			{
				Hide();
			}
		};
		BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
	}

	private void Position()
	{
		Native.GetCursorPos(out var point);
		Native.RECT rECT = Native.WorkArea(point.X, point.Y);
		double num = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
		base.Left = ((double)rECT.Left + (double)(rECT.Right - rECT.Left) / 2.0) / num - base.ActualWidth / 2.0;
		base.Top = (double)rECT.Top / num + 6.0;
	}
}
