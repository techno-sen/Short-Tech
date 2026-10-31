using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShortTech.Core;
using ShortTech.Core.Images;
using ImageMagick;
using Microsoft.Win32;

namespace ShortTech.App.Windows;

public static class AnnotateWindow
{
	private static readonly (AnnotationTool Tool, string Glyph, string Title, string Hint)[] Tools = new(AnnotationTool, string, string, string)[9]
	{
		(AnnotationTool.Arrow, "\ue76c", "Arrow", "Drag from the tail toward the arrowhead. Drag an annotation to move it."),
		(AnnotationTool.Highlight, "\ue7e6", "Highlight", "Drag over the image to highlight."),
		(AnnotationTool.Text, "\ue8d2", "Text", "Click to add a text box, then type below. Drag a box to move it."),
		(AnnotationTool.Rectangle, "\ue739", "Rectangle", "Drag on the image to draw. Drag an annotation to move it."),
		(AnnotationTool.Ellipse, "\uea3a", "Ellipse", "Drag on the image to draw. Drag an annotation to move it."),
		(AnnotationTool.Draw, "\ued63", "Draw", "Drag to draw freehand. Undo removes the last stroke."),
		(AnnotationTool.Callout, "\uf146", "Numbered Callout", "Click to add numbered callouts; drag to move them. Their size stays fixed."),
		(AnnotationTool.Magnifier, "\ue71e", "Magnifier", "Drag out from the spot to magnify. Drag a magnifier to move it."),
		(AnnotationTool.Image, "\ueb9f", "Image", "Paste an image with Ctrl+V or choose one. Drag it to move.")
	};

	private static readonly Color[] Palette = new Color[7]
	{
		Color.FromRgb(237, 66, 54),
		Color.FromRgb(byte.MaxValue, 82, 0),
		Color.FromRgb(byte.MaxValue, 204, 26),
		Color.FromRgb(51, 184, 89),
		Color.FromRgb(41, 120, 245),
		Colors.White,
		Color.FromRgb(20, 20, 20)
	};

	public static Window Make(IReadOnlyList<string> files)
	{
		string file = files[0];
		MagickImage item = Pictures.LoadPreview(file, 1600).Preview;
		ToolWindow window = new ToolWindow("Annotate Photo", 720.0);
		AnnotationCanvas canvas = new AnnotationCanvas
		{
			Photo = Pictures.ToBitmap(item),
			Focusable = true
		};
		Grid grid = new Grid
		{
			Height = 420.0,
			Background = new SolidColorBrush(Color.FromRgb(30, 30, 32)),
			FlowDirection = FlowDirection.LeftToRight
		};
		canvas.HorizontalAlignment = HorizontalAlignment.Center;
		canvas.VerticalAlignment = VerticalAlignment.Center;
		canvas.Margin = new Thickness(8.0);
		grid.Children.Add(canvas);
		TextBlock hint = UI.Caption(Tools[0].Hint);
		TextBox textBox = new TextBox
		{
			Height = 28.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		textBox.TextChanged += delegate
		{
			Annotation selected = canvas.Selected;
			if (selected != null && selected.Tool == AnnotationTool.Text)
			{
				selected.Text = textBox.Text;
				canvas.InvalidateVisual();
			}
		};
		canvas.SelectionChanged += delegate
		{
			Annotation selected = canvas.Selected;
			bool flag = selected != null && selected.Tool == AnnotationTool.Text;
			textBox.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			if (flag && textBox.Text != canvas.Selected.Text)
			{
				textBox.Text = canvas.Selected.Text;
				textBox.Focus();
			}
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		List<Border> toolButtons = new List<Border>();
		for (int num = 0; num < Tools.Length; num++)
		{
			int index = num;
			Border border = new Border
			{
				Width = 34.0,
				Height = 28.0,
				CornerRadius = new CornerRadius(7.0),
				Margin = new Thickness(0.0, 0.0, 4.0, 0.0),
				Cursor = Cursors.Hand,
				ToolTip = L10n.T(Tools[num].Title),
				Child = UI.Icon(Tools[num].Glyph, 14.0)
			};
			((TextBlock)border.Child).HorizontalAlignment = HorizontalAlignment.Center;
			border.MouseLeftButtonUp += delegate
			{
				PickTool(index);
			};
			toolButtons.Add(border);
			stackPanel.Children.Add(border);
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		List<Border> dots = new List<Border>();
		Color[] palette = Palette;
		foreach (Color color in palette)
		{
			Border dot = new Border
			{
				Width = 20.0,
				Height = 20.0,
				CornerRadius = new CornerRadius(10.0),
				Background = new SolidColorBrush(color),
				Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
				BorderBrush = Theme.Hairline,
				BorderThickness = new Thickness(1.0),
				Cursor = Cursors.Hand
			};
			dot.MouseLeftButtonUp += delegate
			{
				canvas.Color = color;
				if (canvas.Selected != null)
				{
					canvas.Selected.Color = color;
					canvas.InvalidateVisual();
				}
				foreach (Border item2 in dots)
				{
					item2.BorderBrush = ((item2 == dot) ? Theme.Accent : Theme.Hairline);
				}
				foreach (Border item3 in dots)
				{
					item3.BorderThickness = new Thickness((item3 == dot) ? 2.5 : 1.0);
				}
			};
			dots.Add(dot);
			stackPanel2.Children.Add(dot);
		}
		stackPanel2.Children.Add(UI.IconButton("\ue7a7", delegate
		{
			if (canvas.Annotations.Count > 0)
			{
				canvas.Annotations.RemoveAt(canvas.Annotations.Count - 1);
				canvas.Selected = null;
				canvas.InvalidateVisual();
			}
		}, "Undo last annotation").Pad(12.0, 0.0));
		stackPanel2.Children.Add(UI.IconButton("\ue74d", delegate
		{
			if (canvas.Selected != null)
			{
				canvas.Annotations.Remove(canvas.Selected);
				canvas.Selected = null;
				canvas.InvalidateVisual();
			}
		}, "Delete selected annotation"));
		window.KeyDown += delegate(object _, KeyEventArgs e)
		{
			if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && System.Windows.Clipboard.ContainsImage())
			{
				AddImage(canvas, System.Windows.Clipboard.GetImage());
			}
			if (e.Key == Key.Delete && !textBox.IsKeyboardFocused && canvas.Selected != null)
			{
				canvas.Annotations.Remove(canvas.Selected);
				canvas.Selected = null;
				canvas.InvalidateVisual();
			}
		};
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Children.Add(grid);
		stackPanel3.Children.Add(stackPanel);
		stackPanel3.Children.Add(stackPanel2);
		stackPanel3.Children.Add(textBox);
		stackPanel3.Children.Add(hint.Pad(0.0, 6.0));
		window.Body = stackPanel3;
		window.Leading(UI.Button("Reset", delegate
		{
			canvas.Annotations.Clear();
			canvas.Selected = null;
			canvas.InvalidateVisual();
		}));
		window.Trailing(UI.Button("Save Annotated Copy", delegate
		{
			List<Annotation> marks = canvas.Annotations.ToList();
			window.Close();
			Runner.Run("Annotate", new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), async delegate(IReadOnlyList<string> f, OperationContext c)
			{
				c.Report("Saving annotated copy");
				byte[] full = await Task.Run(delegate
				{
					using MagickImage magickImage = ImageEngine.Load(f[0]);
					return magickImage.ToByteArray(MagickFormat.Png);
				});
				byte[] png = Application.Current.Dispatcher.Invoke(() => AnnotationRenderer.Render(Pictures.FromBytes(full), marks));
				ImageFormat format = ImageOps.EditedFormat(f[0]);
				string output = Output.PathFor(f[0], format.Ext, "annotated");
				await Task.Run(delegate
				{
					using MagickImage image = new MagickImage(png);
					ImageEngine.Save(image, format, output, 0.92);
				});
				return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
			});
		}, primary: true));
		PickTool(0);
		dots[0].BorderBrush = Theme.Accent;
		dots[0].BorderThickness = new Thickness(2.5);
		return window;
		void PickTool(int num3)
		{
			canvas.Tool = Tools[num3].Tool;
			hint.Text = L10n.T(Tools[num3].Hint);
			for (int i = 0; i < toolButtons.Count; i++)
			{
				toolButtons[i].Background = ((i == num3) ? Theme.Accent : Theme.ButtonFill);
			}
			for (int j = 0; j < toolButtons.Count; j++)
			{
				((TextBlock)toolButtons[j].Child).Foreground = ((j == num3) ? Brushes.White : Theme.Ink);
			}
			if (Tools[num3].Tool == AnnotationTool.Image)
			{
				AddImage(canvas, null);
			}
		}
	}

	private static void AddImage(AnnotationCanvas canvas, BitmapSource? image)
	{
		if (image == null)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp"
			};
			if (openFileDialog.ShowDialog() != true)
			{
				return;
			}
			image = new BitmapImage(new Uri(openFileDialog.FileName));
		}
		double num = ((canvas.Photo == null) ? 1.0 : ((double)canvas.Photo.PixelWidth / (double)canvas.Photo.PixelHeight));
		double num2 = 0.3;
		double num3 = num2 * num * (double)image.PixelHeight / (double)Math.Max(1, image.PixelWidth);
		canvas.Annotations.Add(new Annotation
		{
			Tool = AnnotationTool.Image,
			Image = image,
			A = new Point(0.35, 0.35),
			B = new Point(0.35 + num2, 0.35 + num3)
		});
		canvas.InvalidateVisual();
	}
}
