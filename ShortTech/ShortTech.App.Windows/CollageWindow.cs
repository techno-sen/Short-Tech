using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ShortTech.Core;
using ShortTech.Core.Images;
using ImageMagick;

namespace ShortTech.App.Windows;

public static class CollageWindow
{
	public static Window Make(IReadOnlyList<string> files)
	{
		List<string> order = files.ToList();
		CollageOptions options = new CollageOptions
		{
			Columns = ((files.Count <= 4) ? 2 : 3)
		};
		Dictionary<string, MagickImage> thumbs = order.ToDictionary((string f) => f, (string f) => ImageEngine.Load(f, 900));
		ToolWindow window = new ToolWindow("Create Collage", 640.0);
		PictureStage stage = new PictureStage(340.0)
		{
			Background = new SolidColorBrush(Color.FromArgb(16, 0, 0, 0))
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(stage);
		DispatcherTimer pending = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(120L)
		};
		pending.Tick += async delegate
		{
			pending.Stop();
			CollageOptions current = options;
			List<MagickImage> images = order.Select((string f) => thumbs[f]).ToList();
			PictureStage pictureStage = stage;
			pictureStage.Source = await Task.Run(delegate
			{
				using MagickImage image = Collage.Render(images, current, 900.0 / (double)current.Width);
				return Pictures.ToBitmap(image);
			});
		};
		stackPanel.Children.Add(UI.Row("Layout", UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(CollageStyle, string)>(new(CollageStyle, string)[4]
		{
			(CollageStyle.Grid, "Grid"),
			(CollageStyle.Row, "Row"),
			(CollageStyle.Column, "Column"),
			(CollageStyle.Featured, "Featured")
		}), options.Style, delegate(CollageStyle s)
		{
			options = options with
			{
				Style = s
			};
			Refresh();
		})).Pad(0.0, 6.0));
		stackPanel.Children.Add(UI.Row("Columns", UI.Slider(1.0, Math.Max(2, files.Count), options.Columns, delegate(double v)
		{
			options = options with
			{
				Columns = (int)Math.Round(v)
			};
			Refresh();
		}, (double v) => Math.Round(v).ToString(), null, 160.0, 1.0)));
		stackPanel.Children.Add(UI.Row("Canvas", UI.Picker((from c in Enum.GetValues<CollageCanvas>()
			select (c: c, c.Title())).ToList(), options.Canvas, delegate(CollageCanvas c)
		{
			options = options with
			{
				Canvas = c
			};
			Refresh();
		})));
		stackPanel.Children.Add(UI.Row("Width", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(int, string)>(new(int, string)[4]
		{
			(1080, "1080 px"),
			(2048, "2048 px"),
			(3000, "3000 px"),
			(4096, "4096 px")
		}), options.Width, delegate(int v)
		{
			options = options with
			{
				Width = v
			};
			Refresh();
		})));
		stackPanel.Children.Add(UI.Row("Image fit", UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(true, "Fill cells (crop edges)"),
			(false, "Fit whole images")
		}), selected: true, delegate(bool v)
		{
			options = options with
			{
				Fill = v
			};
			Refresh();
		})));
		stackPanel.Children.Add(UI.Row("Spacing", UI.Slider(0.0, 80.0, options.Spacing, delegate(double v)
		{
			options = options with
			{
				Spacing = v
			};
			Refresh();
		}, (double v) => $"{Math.Round(v)} px")));
		stackPanel.Children.Add(UI.Row("Corners", UI.Slider(0.0, 120.0, options.Corners, delegate(double v)
		{
			options = options with
			{
				Corners = v
			};
			Refresh();
		}, (double v) => $"{Math.Round(v)} px")));
		StackPanel orderList = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		DrawOrder();
		stackPanel.Children.Add(UI.Row("Image order", new ScrollViewer
		{
			Content = orderList,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			MaxWidth = 400.0
		}).Pad(0.0, 6.0));
		window.Body = stackPanel;
		window.Leading(UI.Button("Reset Layout", delegate
		{
			options = new CollageOptions
			{
				Columns = ((files.Count <= 4) ? 2 : 3)
			};
			order = files.ToList();
			DrawOrder();
			Refresh();
		}));
		window.Trailing(UI.Button("Create Collage", delegate
		{
			CollageOptions chosen = options;
			List<string> files2 = order.ToList();
			window.Close();
			Runner.Run("Create Collage", files2, (IReadOnlyList<string> f, OperationContext c) => Collage.Make(f, chosen, c));
		}, primary: true));
		window.Closed += delegate
		{
			foreach (MagickImage value3 in thumbs.Values)
			{
				value3.Dispose();
			}
		};
		Refresh();
		return window;
		void DrawOrder()
		{
			orderList.Children.Clear();
			for (int i = 0; i < order.Count; i++)
			{
				int index = i;
				StackPanel stackPanel2 = new StackPanel
				{
					Margin = new Thickness(0.0, 0.0, 6.0, 0.0)
				};
				stackPanel2.Children.Add(new Image
				{
					Source = Pictures.ToBitmap(Thumb(thumbs[order[i]])),
					Width = 44.0,
					Height = 44.0,
					Stretch = Stretch.UniformToFill
				});
				StackPanel stackPanel3 = new StackPanel
				{
					Orientation = System.Windows.Controls.Orientation.Horizontal,
					HorizontalAlignment = HorizontalAlignment.Center
				};
				stackPanel3.Children.Add(UI.IconButton("\ue76b", delegate
				{
					if (index > 0)
					{
						List<string> list = order;
						int index2 = index - 1;
						List<string> list2 = order;
						int index3 = index;
						string value = order[index];
						string value2 = order[index - 1];
						list[index2] = value;
						list2[index3] = value2;
						DrawOrder();
						Refresh();
					}
				}, "Move", 20.0));
				stackPanel3.Children.Add(UI.IconButton("\ue76c", delegate
				{
					if (index < order.Count - 1)
					{
						List<string> list = order;
						int index2 = index + 1;
						List<string> list2 = order;
						int index3 = index;
						string value = order[index];
						string value2 = order[index + 1];
						list[index2] = value;
						list2[index3] = value2;
						DrawOrder();
						Refresh();
					}
				}, "Move", 20.0));
				stackPanel2.Children.Add(stackPanel3);
				orderList.Children.Add(stackPanel2);
			}
		}
		void Refresh()
		{
			pending.Stop();
			pending.Start();
		}
	}

	private static MagickImage Thumb(MagickImage image)
	{
		MagickImage obj = (MagickImage)image.Clone();
		obj.Thumbnail(88u, 88u);
		return obj;
	}
}
