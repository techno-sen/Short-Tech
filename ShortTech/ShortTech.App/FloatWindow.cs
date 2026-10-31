using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ShortTech.Core;

namespace ShortTech.App;

public sealed class FloatWindow : Window
{
	private readonly ObservableCollection<string> _files = new ObservableCollection<string>();

	private readonly WrapPanel _grid = new WrapPanel
	{
		Margin = new Thickness(8.0)
	};

	private readonly TextBlock _count = UI.Text("", 11.0, FontWeights.SemiBold, Theme.Muted);

	private readonly TextBlock _empty = UI.Text("Drop files here", 12.0, null, Theme.Muted);

	private readonly HashSet<string> _selected = new HashSet<string>();

	private Point? _pressAt;

	private string? _pressed;

	private bool _docked;

	private Rect _undocked;

	public FloatWindow(IReadOnlyList<string> files)
	{
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.Background = Brushes.Transparent;
		base.ShowInTaskbar = false;
		base.Topmost = true;
		base.ResizeMode = ResizeMode.CanResizeWithGrip;
		base.Width = 300.0;
		base.Height = 260.0;
		base.MinWidth = 200.0;
		base.MinHeight = 150.0;
		base.FlowDirection = Theme.Direction;
		base.FontFamily = Theme.Font;
		Native.GetCursorPos(out var point);
		double num = 1.0;
		base.Left = (double)point.X / num - 150.0;
		base.Top = (double)point.Y / num - 60.0;
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(12.0, 8.0, 8.0, 4.0),
			LastChildFill = false
		};
		TextBlock element = UI.Text("Float", 11.0, FontWeights.Bold, Theme.Muted);
		DockPanel.SetDock(element, Dock.Left);
		dockPanel.Children.Add(element);
		_count.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		DockPanel.SetDock(_count, Dock.Left);
		dockPanel.Children.Add(_count);
		(string, string, Action)[] array = new(string, string, Action)[3]
		{
			("\ue711", "Close Float without deleting files", base.Close),
			("\ue842", "Attach to the screen edge", ToggleDock),
			("\ue72d", "Share", delegate
			{
				if (Targets().Count > 0)
				{
					ShareUI.Share(Targets());
				}
			})
		};
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			(string, string, Action) tuple = array[num2];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Action item3 = tuple.Item3;
			Button button = UI.IconButton(item, item3, item2, 24.0);
			button.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
			DockPanel.SetDock(button, Dock.Right);
			dockPanel.Children.Add(button);
		}
		dockPanel.MouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 1)
			{
				DragMove();
			}
		};
		ScrollViewer element2 = new ScrollViewer
		{
			Content = _grid,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		_empty.HorizontalAlignment = HorizontalAlignment.Center;
		Grid element3 = new Grid
		{
			Children = 
			{
				(UIElement)_empty,
				(UIElement)element2
			}
		};
		DockPanel dockPanel2 = new DockPanel();
		DockPanel.SetDock(dockPanel, Dock.Top);
		dockPanel2.Children.Add(dockPanel);
		dockPanel2.Children.Add(element3);
		base.Content = new Border
		{
			Margin = new Thickness(12.0),
			CornerRadius = new CornerRadius(16.0),
			Child = dockPanel2,
			Background = new SolidColorBrush(Color.FromArgb(242, 250, 250, 251)),
			BorderBrush = Theme.Hairline,
			BorderThickness = new Thickness(1.0),
			Effect = new DropShadowEffect
			{
				BlurRadius = 24.0,
				ShadowDepth = 6.0,
				Direction = 270.0,
				Opacity = 0.2
			}
		};
		base.AllowDrop = true;
		base.DragOver += delegate(object _, DragEventArgs e)
		{
			e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
			e.Handled = true;
		};
		base.Drop += delegate(object _, DragEventArgs e)
		{
			if (e.Data.GetData(DataFormats.FileDrop) is string[] files2)
			{
				Add(files2);
			}
			e.Handled = true;
		};
		base.KeyDown += delegate(object _, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
			{
				Remove(Targets());
			}
			if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
			{
				_selected.UnionWith(_files);
				Draw();
			}
		};
		base.SourceInitialized += delegate
		{
			Native.RoundCorners(new WindowInteropHelper(this).Handle);
		};
		Add(files);
	}

	public void Add(IEnumerable<string> files)
	{
		foreach (string file in files)
		{
			if (!_files.Contains(file))
			{
				_files.Add(file);
			}
		}
		Draw();
	}

	private void Remove(IEnumerable<string> files)
	{
		foreach (string item in files.ToList())
		{
			_files.Remove(item);
			_selected.Remove(item);
		}
		Draw();
	}

	private List<string> Targets()
	{
		if (_selected.Count <= 0)
		{
			return _files.ToList();
		}
		return _files.Where(_selected.Contains).ToList();
	}

	private void Draw()
	{
		_grid.Children.Clear();
		_count.Text = ((_files.Count <= 0) ? "" : ((_files.Count == 1) ? L10n.T("1 file") : L10n.T($"{_files.Count} files")));
		_empty.Visibility = ((_files.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
		foreach (string file in _files)
		{
			_grid.Children.Add(Tile(file));
		}
	}

	private FrameworkElement Tile(string file)
	{
		bool flag = _selected.Contains(file);
		Image element = new Image
		{
			Width = 64.0,
			Height = 64.0,
			Source = ShellThumbnail.For(file),
			Stretch = Stretch.Uniform
		};
		TextBlock element2 = new TextBlock
		{
			Text = Path.GetFileName(file.TrimEnd('\\')),
			FontFamily = Theme.Font,
			FontSize = 11.0,
			Foreground = Theme.Ink,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxWidth = 78.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0),
			FlowDirection = FlowDirection.LeftToRight
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(element);
		stackPanel.Children.Add(element2);
		Border tile = new Border
		{
			Child = stackPanel,
			Padding = new Thickness(6.0),
			Margin = new Thickness(3.0),
			CornerRadius = new CornerRadius(9.0),
			Width = 90.0,
			Background = (flag ? Theme.AccentSoft : Brushes.Transparent),
			ToolTip = L10n.T(Path.GetFileName(file) + ". Double-click to open; drag to carry the file.")
		};
		tile.MouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				Process.Start(new ProcessStartInfo(file)
				{
					UseShellExecute = true
				});
			}
			else
			{
				if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
				{
					if (!_selected.Remove(file))
					{
						_selected.Add(file);
					}
				}
				else if (!_selected.Contains(file))
				{
					_selected.Clear();
					_selected.Add(file);
				}
				_pressAt = e.GetPosition(this);
				_pressed = file;
				Draw();
			}
		};
		tile.MouseMove += delegate(object _, MouseEventArgs e)
		{
			Point? pressAt = _pressAt;
			if (pressAt.HasValue)
			{
				Point valueOrDefault = pressAt.GetValueOrDefault();
				if (_pressed != null && e.LeftButton == MouseButtonState.Pressed)
				{
					Point position = e.GetPosition(this);
					if (!(Math.Abs(position.X - valueOrDefault.X) < SystemParameters.MinimumHorizontalDragDistance) || !(Math.Abs(position.Y - valueOrDefault.Y) < SystemParameters.MinimumVerticalDragDistance))
					{
						_pressAt = null;
						DataObject data = new DataObject(DataFormats.FileDrop, Targets().ToArray());
						DragDrop.DoDragDrop(tile, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
					}
				}
			}
		};
		tile.MouseLeftButtonUp += delegate
		{
			_pressAt = null;
		};
		ContextMenu menu = new ContextMenu
		{
			FlowDirection = Theme.Direction
		};
		Item("Open", delegate
		{
			Process.Start(new ProcessStartInfo(file)
			{
				UseShellExecute = true
			});
		});
		Item("Show in File Explorer", delegate
		{
			FileActions.ShowInExplorer(file);
		});
		Item("Copy Path", delegate
		{
			ShortTech.Core.Clipboard.CopyText(file);
		});
		Item("Remove from Float", delegate
		{
			Remove(new global::_003C_003Ez__ReadOnlySingleElementList<string>(file));
		});
		tile.ContextMenu = menu;
		return tile;
		void Item(string title, Action action)
		{
			MenuItem menuItem = new MenuItem
			{
				Header = L10n.T(title)
			};
			menuItem.Click += delegate
			{
				action();
			};
			menu.Items.Add(menuItem);
		}
	}

	private void ToggleDock()
	{
		if (_docked)
		{
			base.Left = _undocked.Left;
			base.Top = _undocked.Top;
			base.Width = _undocked.Width;
			base.Height = _undocked.Height;
			_docked = false;
		}
		else
		{
			_undocked = new Rect(base.Left, base.Top, base.Width, base.Height);
			Native.GetCursorPos(out var point);
			Native.RECT rECT = Native.WorkArea(point.X, point.Y);
			double num = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
			base.Width = 140.0;
			base.Height = Math.Min(560.0, (double)(rECT.Bottom - rECT.Top) / num - 40.0);
			base.Left = (double)rECT.Right / num - base.Width + 4.0;
			base.Top = (double)rECT.Top / num + ((double)(rECT.Bottom - rECT.Top) / num - base.Height) / 2.0;
			_docked = true;
		}
	}
}
