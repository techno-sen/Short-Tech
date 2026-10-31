using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using ShortTech.Core;

namespace ShortTech.App;

public class ToolWindow : Window
{
	private readonly ContentControl _body = new ContentControl();

	private readonly DockPanel _footer = new DockPanel
	{
		LastChildFill = false,
		Margin = new Thickness(16.0, 0.0, 16.0, 0.0),
		Height = 52.0
	};

	private readonly StackPanel _leading = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly StackPanel _trailing = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		VerticalAlignment = VerticalAlignment.Center
	};

	public UIElement Body
	{
		set
		{
			_body.Content = value;
		}
	}

	public ToolWindow(string title, double width)
	{
		base.Title = L10n.T(title);
		base.Width = width;
		base.SizeToContent = SizeToContent.Height;
		base.ResizeMode = ResizeMode.NoResize;
		base.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		base.Background = Theme.Surface;
		base.FontFamily = Theme.Font;
		base.FlowDirection = Theme.Direction;
		base.Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/ShortTech.ico"));
		WindowChrome.SetWindowChrome(this, new WindowChrome
		{
			CaptionHeight = 44.0,
			ResizeBorderThickness = new Thickness(0.0),
			GlassFrameThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			UseAeroCaptionButtons = false
		});
		Grid grid = new Grid
		{
			Height = 44.0
		};
		grid.Children.Add(new TextBlock
		{
			Text = base.Title,
			FontFamily = Theme.Font,
			FontSize = 15.0,
			FontWeight = FontWeights.SemiBold,
			Foreground = Theme.Ink,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});
		Button button = UI.IconButton("\ue711", base.Close, "Close", 24.0);
		button.HorizontalAlignment = HorizontalAlignment.Left;
		button.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		WindowChrome.SetIsHitTestVisibleInChrome(button, hitTestVisible: true);
		grid.Children.Add(button);
		DockPanel.SetDock(_leading, Dock.Left);
		DockPanel.SetDock(_trailing, Dock.Right);
		_footer.Children.Add(_leading);
		_footer.Children.Add(_trailing);
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = true
		};
		DockPanel.SetDock(grid, Dock.Top);
		dockPanel.Children.Add(grid);
		Border element = new Border
		{
			Height = 1.0,
			Background = Theme.Hairline
		};
		DockPanel.SetDock(element, Dock.Top);
		dockPanel.Children.Add(element);
		DockPanel.SetDock(_footer, Dock.Bottom);
		dockPanel.Children.Add(_footer);
		Border element2 = new Border
		{
			Height = 1.0,
			Background = Theme.Hairline
		};
		DockPanel.SetDock(element2, Dock.Bottom);
		dockPanel.Children.Add(element2);
		_body.Margin = new Thickness(16.0);
		dockPanel.Children.Add(_body);
		base.Content = dockPanel;
		base.KeyDown += delegate(object _, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
			}
		};
		base.SourceInitialized += delegate
		{
			Native.RoundCorners(new WindowInteropHelper(this).Handle);
		};
	}

	public void Leading(params UIElement[] items)
	{
		foreach (UIElement uIElement in items)
		{
			if (uIElement is FrameworkElement frameworkElement)
			{
				frameworkElement.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			}
			_leading.Children.Add(uIElement);
		}
	}

	public void Trailing(params UIElement[] items)
	{
		foreach (UIElement uIElement in items)
		{
			if (uIElement is FrameworkElement frameworkElement)
			{
				frameworkElement.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
			}
			_trailing.Children.Add(uIElement);
		}
	}

	public void Present()
	{
		Show();
		Activate();
		base.Topmost = true;
		base.Topmost = false;
		Focus();
	}
}
