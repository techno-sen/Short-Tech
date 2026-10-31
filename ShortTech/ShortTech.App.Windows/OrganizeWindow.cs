using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using ShortTech.Core;
using ShortTech.Core.Pdf;
using ImageMagick;

namespace ShortTech.App.Windows;

public static class OrganizeWindow
{
	private const string DragFormat = "ShortTech.PageSlot";

	public static Window Make(IReadOnlyList<string> files)
	{
		string file = files[0];
		int count = PdfOps.PageCount(file);
		List<PageSlot> original = (from i in Enumerable.Range(0, count)
			select new PageSlot(i)).ToList();
		List<PageSlot> slots = original.ToList();
		Dictionary<int, BitmapSource> thumbnails = new Dictionary<int, BitmapSource>();
		bool equalWidths = false;
		ToolWindow window = new ToolWindow("Organize PDF", 580.0);
		WrapPanel grid = new WrapPanel
		{
			Margin = new Thickness(4.0)
		};
		Brush muted = Theme.Muted;
		TextBlock pageCount = UI.Text("", 11.0, null, muted);
		Button save = UI.Button("Save Organized PDF", delegate
		{
		}, primary: true);
		Button reset = UI.Button("Reset Order", delegate
		{
		});
		StackPanel stackPanel = new StackPanel();
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false
		};
		DockPanel.SetDock(pageCount, Dock.Left);
		dockPanel.Children.Add(pageCount);
		reset.Click += delegate
		{
			slots = original.ToList();
			Build();
		};
		DockPanel.SetDock(reset, Dock.Right);
		dockPanel.Children.Add(reset);
		stackPanel.Children.Add(dockPanel);
		stackPanel.Children.Add(new ScrollViewer
		{
			Content = grid,
			Height = 380.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		});
		stackPanel.Children.Add(UI.Divider());
		stackPanel.Children.Add(UI.Check("Make all pages the same width", value: false, delegate(bool v)
		{
			equalWidths = v;
			save.IsEnabled = Changed() || equalWidths;
		}));
		stackPanel.Children.Add(UI.Caption("Pages are scaled proportionally to match the narrowest page; their heights may still differ. Drag pages to reorder them; right-click for more.").Pad(0.0, 6.0));
		window.Body = stackPanel;
		save.Click += delegate
		{
			List<PageSlot> order = slots.ToList();
			bool equal = equalWidths;
			window.Close();
			Runner.Run("Organize PDF", new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await PdfOps.Organize(f[0], order, equal, c)));
		};
		window.Trailing(save);
		Build();
		Task.Run(delegate
		{
			for (int i = 0; i < Math.Min(count, 300); i++)
			{
				byte[] png;
				try
				{
					using MagickImage magickImage = PdfOps.RenderPage(file, i, 36);
					png = magickImage.ToByteArray(MagickFormat.Png);
				}
				catch
				{
					continue;
				}
				int index = i;
				window.Dispatcher.BeginInvoke((Action)delegate
				{
					BitmapSource bitmapSource = Pictures.FromBytes(png);
					if (bitmapSource != null)
					{
						thumbnails[index] = bitmapSource;
						for (int j = 0; j < slots.Count && j < grid.Children.Count; j++)
						{
							if (slots[j].Source == index && grid.Children[j] is Grid grid2 && grid2.Children[1] is Image image)
							{
								image.Source = bitmapSource;
							}
						}
					}
				});
			}
		});
		return window;
		void Build()
		{
			grid.Children.Clear();
			pageCount.Text = L10n.T((slots.Count == 1) ? "1 page" : $"{slots.Count} pages");
			for (int i = 0; i < slots.Count; i++)
			{
				PageSlot slot = slots[i];
				Grid card = new Grid
				{
					Width = 122.0,
					Height = 150.0,
					Margin = new Thickness(7.0),
					Background = Brushes.Transparent,
					AllowDrop = true,
					Cursor = Cursors.Hand
				};
				card.Children.Add(new Border
				{
					Background = Brushes.White,
					CornerRadius = new CornerRadius(8.0),
					Effect = new DropShadowEffect
					{
						BlurRadius = 6.0,
						ShadowDepth = 1.0,
						Direction = 270.0,
						Opacity = 0.18
					}
				});
				Image image = new Image
				{
					Margin = new Thickness(10.0),
					Stretch = Stretch.Uniform,
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new RotateTransform(slot.Rotation)
				};
				if (thumbnails.TryGetValue(slot.Source, out BitmapSource value))
				{
					image.Source = value;
				}
				card.Children.Add(image);
				DockPanel dockPanel2 = new DockPanel
				{
					Margin = new Thickness(6.0),
					VerticalAlignment = VerticalAlignment.Top,
					LastChildFill = false
				};
				Border element = new Border
				{
					Width = 20.0,
					Height = 20.0,
					CornerRadius = new CornerRadius(10.0),
					Background = Theme.Accent,
					Child = new TextBlock
					{
						Text = L10n.T($"{i + 1}"),
						FontSize = 10.0,
						FontWeight = FontWeights.Bold,
						Foreground = Brushes.White,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				};
				DockPanel.SetDock(element, Dock.Left);
				dockPanel2.Children.Add(element);
				Border border = new Border
				{
					Width = 20.0,
					Height = 20.0,
					CornerRadius = new CornerRadius(10.0),
					Background = new SolidColorBrush(Color.FromArgb(153, 0, 0, 0)),
					ToolTip = L10n.T("Rotate clockwise"),
					Child = UI.Icon("\ue7ad", 10.0, Brushes.White)
				};
				((TextBlock)border.Child).HorizontalAlignment = HorizontalAlignment.Center;
				border.MouseLeftButtonUp += delegate(object _, MouseButtonEventArgs e)
				{
					Rotate(slot);
					e.Handled = true;
				};
				DockPanel.SetDock(border, Dock.Right);
				dockPanel2.Children.Add(border);
				card.Children.Add(dockPanel2);
				ContextMenu contextMenu = new ContextMenu
				{
					Items = 
					{
						(object)Item("Rotate Clockwise", delegate
						{
							Rotate(slot);
						}),
						(object)Item("Duplicate Page", delegate
						{
							Duplicate(slot);
						}),
						(object)new Separator(),
						(object)Item("Delete Page", delegate
						{
							Remove(slot);
						}, slots.Count > 1)
					}
				};
				card.ContextMenu = contextMenu;
				Point? down = null;
				card.PreviewMouseLeftButtonDown += delegate(object _, MouseButtonEventArgs e)
				{
					down = e.GetPosition(card);
				};
				card.PreviewMouseMove += delegate(object _, MouseEventArgs e)
				{
					if (down.HasValue)
					{
						Point valueOrDefault = down.GetValueOrDefault();
						if (e.LeftButton == MouseButtonState.Pressed && !((e.GetPosition(card) - valueOrDefault).Length < 6.0))
						{
							down = null;
							card.Opacity = 0.4;
							DragDrop.DoDragDrop(card, new DataObject("ShortTech.PageSlot", slot.Id.ToString()), DragDropEffects.Move);
							card.Opacity = 1.0;
							Build();
						}
					}
				};
				card.DragOver += delegate(object _, DragEventArgs e)
				{
					e.Effects = (e.Data.GetDataPresent("ShortTech.PageSlot") ? DragDropEffects.Move : DragDropEffects.None);
					e.Handled = true;
				};
				card.DragEnter += delegate(object _, DragEventArgs e)
				{
					object data = e.Data.GetData("ShortTech.PageSlot");
					string id = data as string;
					if (id != null)
					{
						PageSlot pageSlot = slots.FirstOrDefault((PageSlot s) => s.Id.ToString() == id);
						if (!(pageSlot == null) && !(pageSlot == slot))
						{
							int index = slots.IndexOf(pageSlot);
							int index2 = slots.IndexOf(slot);
							slots.RemoveAt(index);
							slots.Insert(index2, pageSlot);
							UIElement element2 = grid.Children[index];
							grid.Children.RemoveAt(index);
							grid.Children.Insert(index2, element2);
						}
					}
				};
				grid.Children.Add(card);
			}
			reset.IsEnabled = Changed();
			save.IsEnabled = Changed() || equalWidths;
		}
		bool Changed()
		{
			if (slots.Select((PageSlot s) => s.Source).SequenceEqual(original.Select((PageSlot s) => s.Source)))
			{
				return slots.Any((PageSlot s) => s.Rotation != 0);
			}
			return true;
		}
		void Duplicate(PageSlot slot)
		{
			int num = slots.IndexOf(slot);
			if (num >= 0)
			{
				slots.Insert(num + 1, new PageSlot(slot.Source, slot.Rotation));
			}
			Build();
		}
		static MenuItem Item(string title, Action act, bool enabled = true)
		{
			MenuItem menuItem = new MenuItem();
			menuItem.Header = L10n.T(title);
			menuItem.IsEnabled = enabled;
			menuItem.Click += delegate
			{
				act();
			};
			return menuItem;
		}
		void Remove(PageSlot slot)
		{
			if (slots.Count > 1)
			{
				slots.Remove(slot);
				Build();
			}
		}
		void Rotate(PageSlot slot)
		{
			int num = slots.IndexOf(slot);
			if (num >= 0)
			{
				slots[num] = slot with
				{
					Rotation = (slot.Rotation + 90) % 360
				};
			}
			Build();
		}
	}
}
