using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using ShortTech.Core;

namespace ShortTech.App.Wheel;

public sealed class WheelWindow : Window
{
	private readonly Grid _root = new Grid
	{
		Width = 300.0,
		Height = 300.0,
		Background = Brushes.Transparent
	};

	private readonly Canvas _petals = new Canvas
	{
		Width = 300.0,
		Height = 300.0,
		IsHitTestVisible = false
	};

	private readonly TextBlock _center = new TextBlock
	{
		TextAlignment = TextAlignment.Center,
		TextWrapping = TextWrapping.Wrap,
		Foreground = Brushes.Black,
		MaxWidth = 64.0
	};

	private readonly ScaleTransform _scale = new ScaleTransform(0.9, 0.9, 150.0, 150.0);

	private IReadOnlyList<ShortTechOperation> _operations = Array.Empty<ShortTechOperation>();

	private IReadOnlyList<string> _files = Array.Empty<string>();

	private int? _hovered;

	private bool _shown;

	public WheelMode Mode { get; private set; }

	public bool IsWheelShown => _shown;

	public WheelWindow()
	{
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.Background = Brushes.Transparent;
		base.ShowInTaskbar = false;
		base.ShowActivated = false;
		base.Topmost = true;
		base.ResizeMode = ResizeMode.NoResize;
		base.SizeToContent = SizeToContent.WidthAndHeight;
		base.FlowDirection = FlowDirection.LeftToRight;
		Ellipse ellipse = new Ellipse
		{
			Width = 252.0,
			Height = 252.0,
			Stroke = new SolidColorBrush(Color.FromArgb(85, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			StrokeThickness = 1.0,
			Fill = Theme.Disc,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false,
			Effect = new DropShadowEffect
			{
				BlurRadius = 18.0,
				ShadowDepth = 5.0,
				Direction = 270.0,
				Opacity = 0.18,
				Color = Color.FromRgb(38, 52, 56)
			}
		};
		EllipseGeometry geometry = new EllipseGeometry(new Point(150.0, 150.0), 42.6, 42.6);
		ellipse.OpacityMask = new DrawingBrush(new GeometryDrawing(Brushes.Black, null, new CombinedGeometry(GeometryCombineMode.Exclude, new EllipseGeometry(new Point(150.0, 150.0), 126.0, 126.0), geometry)))
		{
			Stretch = Stretch.None,
			ViewboxUnits = BrushMappingMode.Absolute,
			Viewbox = new Rect(24.0, 24.0, 252.0, 252.0)
		};
		Border element = new Border
		{
			Child = _center,
			Background = new SolidColorBrush(Color.FromArgb(238, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			CornerRadius = new CornerRadius(24.0),
			BorderBrush = new SolidColorBrush(Color.FromArgb(179, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			MinHeight = 32.0,
			MinWidth = 70.0,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Effect = new DropShadowEffect
			{
				BlurRadius = 6.0,
				ShadowDepth = 2.0,
				Direction = 270.0,
				Opacity = 0.1
			}
		};
		_center.VerticalAlignment = VerticalAlignment.Center;
		_root.Children.Add(ellipse);
		_root.Children.Add(_petals);
		_root.Children.Add(element);
		_root.RenderTransform = _scale;
		_root.AllowDrop = true;
		_root.DragEnter += delegate(object _, DragEventArgs e)
		{
			OnDrag(e, entering: true);
		};
		_root.DragOver += delegate(object _, DragEventArgs e)
		{
			OnDrag(e, entering: false);
		};
		_root.DragLeave += delegate(object _, DragEventArgs e)
		{
			Point position = e.GetPosition(_root);
			if (position.X <= 0.0 || position.Y <= 0.0 || position.X >= _root.ActualWidth || position.Y >= _root.ActualHeight)
			{
				SetHovered(null);
			}
		};
		_root.Drop += OnDrop;
		base.Content = _root;
		base.SourceInitialized += delegate
		{
			Native.MakeToolWindow(new WindowInteropHelper(this).Handle);
		};
	}

	public void Show(WheelMode mode, Point screenPixels)
	{
		Mode = mode;
		if (!_shown)
		{
			_files = Array.Empty<string>();
			_operations = Array.Empty<ShortTechOperation>();
			base.Opacity = 0.0;
			if (!base.IsLoaded)
			{
				base.Left = -10000.0;
				base.Top = -10000.0;
				Show();
			}
			Place(screenPixels);
			Show();
			_shown = true;
			BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120L)));
			DoubleAnimation animation = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(220L))
			{
				EasingFunction = new BackEase
				{
					Amplitude = 0.3,
					EasingMode = EasingMode.EaseOut
				}
			};
			_scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
			_scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
		}
		else if (_files.Count > 0)
		{
			_operations = Catalog.For(mode, _files);
		}
		_hovered = null;
		Draw();
		Sounds.Tick();
	}

	public void HideWheel()
	{
		if (!_shown)
		{
			return;
		}
		_shown = false;
		DoubleAnimation doubleAnimation = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(120L));
		doubleAnimation.Completed += delegate
		{
			if (!_shown)
			{
				Hide();
			}
		};
		BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
	}

	private void Place(Point screenPixels)
	{
		Matrix matrix = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
		double num = ((matrix.M11 <= 0.0) ? 1.0 : matrix.M11);
		Native.RECT rECT = Native.WorkArea((int)screenPixels.X, (int)screenPixels.Y);
		double num2 = 300.0 * num;
		double num3 = Math.Clamp(screenPixels.X - num2 / 2.0, rECT.Left, (double)rECT.Right - num2);
		double num4 = Math.Clamp(screenPixels.Y - num2 / 2.0, rECT.Top, (double)rECT.Bottom - num2);
		base.Left = num3 / num;
		base.Top = num4 / num;
	}

	private void OnDrag(DragEventArgs e, bool entering)
	{
		e.Handled = true;
		if (entering && e.Data.GetData(DataFormats.FileDrop) is string[] array && array.Length != 0)
		{
			_files = array;
			_operations = Catalog.For(Mode, _files);
			_hovered = null;
			Draw();
		}
		int? hovered = ((_operations.Count > 0) ? WheelGeometry.Index(e.GetPosition(_root), _operations.Count) : ((int?)null));
		SetHovered(hovered);
		e.Effects = ((hovered.HasValue && e.AllowedEffects.HasFlag(DragDropEffects.Copy)) ? DragDropEffects.Copy : ((hovered.HasValue && e.AllowedEffects.HasFlag(DragDropEffects.Link)) ? DragDropEffects.Link : DragDropEffects.None));
	}

	private void OnDrop(object sender, DragEventArgs e)
	{
		e.Handled = true;
		int? hovered = _hovered;
		object obj;
		if (hovered.HasValue)
		{
			int valueOrDefault = hovered.GetValueOrDefault();
			if (valueOrDefault < _operations.Count)
			{
				obj = _operations[valueOrDefault];
				goto IL_0043;
			}
		}
		obj = null;
		goto IL_0043;
		IL_0043:
		ShortTechOperation operation = (ShortTechOperation)obj;
		e.Effects = ((operation != null) ? DragDropEffects.Copy : DragDropEffects.None);
		string[] files = (e.Data.GetData(DataFormats.FileDrop) as string[]) ?? _files.ToArray();
		HideWheel();
		if (!(operation == null) && files.Length != 0)
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				Runner.Start(operation, files);
			}, DispatcherPriority.Background);
		}
	}

	private void SetHovered(int? index)
	{
		if (index != _hovered)
		{
			_hovered = index;
			Draw();
			if (index.HasValue)
			{
				Sounds.Tick();
			}
		}
	}

	private void Draw()
	{
		_petals.Children.Clear();
		int count = _operations.Count;
		for (int i = 0; i < count; i++)
		{
			bool flag = i == _hovered;
			ShortTechOperation operation = _operations[i];
			Path element = new Path
			{
				Data = WheelGeometry.Petal(i, count),
				Fill = (flag ? Theme.Accent : Theme.PetalFill),
				Stroke = (flag ? new SolidColorBrush(Color.FromArgb(112, 62, 46, 170)) : Theme.PetalStroke),
				StrokeThickness = 0.75
			};
			Canvas canvas = new Canvas
			{
				Children = { (UIElement)element }
			};
			FrameworkElement frameworkElement = Label(operation, flag, count);
			Point point = WheelGeometry.Position(i, count);
			frameworkElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Canvas.SetLeft(frameworkElement, point.X - frameworkElement.DesiredSize.Width / 2.0);
			Canvas.SetTop(frameworkElement, point.Y - frameworkElement.DesiredSize.Height / 2.0);
			canvas.Children.Add(frameworkElement);
			if (flag && count > 1)
			{
				double num = -Math.PI / 2.0 + (double)i / (double)count * 2.0 * Math.PI;
				canvas.RenderTransform = new TranslateTransform(Math.Cos(num) * 5.0, Math.Sin(num) * 5.0);
				canvas.Effect = new DropShadowEffect
				{
					Color = Theme.AccentColor,
					BlurRadius = 12.0,
					ShadowDepth = 3.0,
					Direction = 270.0,
					Opacity = 0.35
				};
			}
			else
			{
				canvas.Effect = new DropShadowEffect
				{
					BlurRadius = 10.0,
					ShadowDepth = 3.0,
					Direction = 270.0,
					Opacity = 0.12,
					Color = Color.FromRgb(38, 52, 56)
				};
			}
			_petals.Children.Add(canvas);
		}
		int? hovered = _hovered;
		object obj;
		if (hovered.HasValue)
		{
			int valueOrDefault = hovered.GetValueOrDefault();
			if (valueOrDefault < count)
			{
				obj = _operations[valueOrDefault];
				goto IL_02b4;
			}
		}
		obj = null;
		goto IL_02b4;
		IL_02b4:
		ShortTechOperation shortTechOperation = (ShortTechOperation)obj;
		bool flag2 = (object)shortTechOperation != null && shortTechOperation.Badge == null;
		_center.Text = shortTechOperation?.WheelLabel ?? ((_files.Count > 0 && count == 0) ? L10n.Upper(L10n.T("NO OPTIONS")) : L10n.Upper(L10n.T(ModeTitle(Mode))));
		_center.FontFamily = Theme.Font;
		_center.FontSize = ((flag2 || shortTechOperation == null) ? 10 : 13);
		_center.FontWeight = ((flag2 || shortTechOperation == null) ? FontWeights.SemiBold : FontWeights.Medium);
	}

	public FrameworkElement Preview(WheelMode mode, IReadOnlyList<string> files, int? hovered)
	{
		Mode = mode;
		_files = files;
		_operations = Catalog.For(mode, files);
		_hovered = hovered;
		ScaleTransform scale = _scale;
		double scaleX = (_scale.ScaleY = 1.0);
		scale.ScaleX = scaleX;
		Draw();
		base.Content = null;
		return _root;
	}

	public static string ModeTitle(WheelMode mode)
	{
		return mode switch
		{
			WheelMode.Convert => "Convert", 
			WheelMode.Tools => "Tools", 
			_ => "Actions", 
		};
	}

	private static FrameworkElement Label(ShortTechOperation operation, bool hovered, int count)
	{
		SolidColorBrush foreground = (hovered ? Brushes.White : Brushes.Black);
		string badge = operation.Badge;
		if (badge != null)
		{
			return new TextBlock
			{
				Text = badge.ToUpperInvariant(),
				FontFamily = Theme.Font,
				FontWeight = FontWeights.SemiBold,
				FontSize = ((count > 8) ? 12.5 : 14.5),
				Foreground = foreground,
				IsHitTestVisible = false
			};
		}
		return new StackPanel
		{
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			Children = 
			{
				(UIElement)new TextBlock
				{
					Text = operation.Icon,
					FontFamily = Theme.Icons,
					FontSize = 15.0,
					Foreground = foreground,
					HorizontalAlignment = HorizontalAlignment.Center
				},
				(UIElement)new TextBlock
				{
					Text = operation.WheelLabel,
					FontFamily = Theme.Font,
					FontWeight = FontWeights.SemiBold,
					FontSize = 9.5,
					Foreground = foreground,
					HorizontalAlignment = HorizontalAlignment.Center,
					Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
					MaxWidth = 66.0,
					TextTrimming = TextTrimming.CharacterEllipsis,
					FlowDirection = Theme.Direction
				}
			}
		};
	}
}
