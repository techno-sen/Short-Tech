using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.Core;
using ShortTech.Core.Images;
using ImageMagick;
using Microsoft.Win32;

namespace ShortTech.App.Windows;

public static class QrWindow
{
	private sealed class Model
	{
		public bool Wifi;

		public string Text = "";

		public string Network = "";

		public string Password = "";

		public QrContent.Security Security;

		public bool Hidden;

		public QrMatrix.Correction Correction = QrMatrix.Correction.Medium;

		public QrDesign.ModuleShape Module;

		public QrDesign.EyeShape Eye;

		public Color Foreground = Colors.Black;

		public Color EyeColor = Colors.Black;

		public Color GradientEnd = Theme.AccentColor;

		public Color Background = Colors.White;

		public bool SeparateEye;

		public bool Gradient;

		public bool Transparent;

		public int Margin = 4;

		public BitmapSource? Logo;

		public double LogoScale = 0.22;

		public QrDesign.Plate Plate = QrDesign.Plate.Rounded;

		public int Pixels = 1024;

		public bool Svg;

		public string Payload
		{
			get
			{
				if (Wifi)
				{
					if (Network.Length != 0)
					{
						return QrContent.Wifi(Network, Password, Security, Hidden);
					}
					return "";
				}
				return Text;
			}
		}

		public QrMatrix? Matrix => QrMatrix.Encode(Payload, Correction);

		public QrDesign Design => new QrDesign
		{
			Module = Module,
			Eye = Eye,
			Foreground = Foreground,
			GradientEnd = (Gradient ? new Color?(GradientEnd) : ((Color?)null)),
			EyeColor = (SeparateEye ? EyeColor : Foreground),
			Background = (Transparent ? ((Color?)null) : new Color?(Background)),
			Margin = Margin,
			Logo = Logo,
			LogoScale = LogoScale,
			LogoPlate = Plate
		};

		public string? Problem
		{
			get
			{
				if (Payload.Length == 0)
				{
					if (!Wifi)
					{
						return "Type the text or link to encode.";
					}
					return "Enter the network name.";
				}
				if (Matrix != null)
				{
					return null;
				}
				return "This is too long for a QR code. Shorten it or lower the error correction.";
			}
		}
	}

	public static Window Make(IReadOnlyList<string> files)
	{
		string source = ((files.Count > 0) ? files[0] : null);
		Model model = new Model();
		if (source != null)
		{
			model.Text = QrContent.Text(source) ?? "";
		}
		else if (System.Windows.Clipboard.ContainsText())
		{
			string text = System.Windows.Clipboard.GetText().Trim();
			if (text != null && text.Length <= 800)
			{
				model.Text = text;
			}
		}
		ToolWindow window = new ToolWindow("Create QR Code", 720.0);
		StackPanel stackPanel = new StackPanel
		{
			Width = 360.0
		};
		Image preview = new Image
		{
			Width = 258.0,
			Height = 258.0,
			Stretch = Stretch.Uniform
		};
		RenderOptions.SetBitmapScalingMode(preview, BitmapScalingMode.HighQuality);
		TextBlock placeholder = UI.Icon("\ued14", 64.0, Theme.Muted);
		placeholder.HorizontalAlignment = HorizontalAlignment.Center;
		placeholder.Opacity = 0.5;
		Border previewBox = new Border
		{
			Width = 290.0,
			Height = 290.0,
			CornerRadius = new CornerRadius(14.0),
			Background = Theme.Card,
			AllowDrop = true,
			BorderThickness = new Thickness(3.0),
			BorderBrush = Brushes.Transparent
		};
		Grid grid = new Grid();
		grid.Children.Add(placeholder);
		grid.Children.Add(preview);
		previewBox.Child = grid;
		TextBlock status = UI.Text("", 12.0, FontWeights.Medium, null, wrap: true);
		status.TextAlignment = System.Windows.TextAlignment.Center;
		status.Width = 280.0;
		Brush muted = Theme.Muted;
		TextBlock modules = UI.Text("", 11.0, null, muted);
		modules.HorizontalAlignment = HorizontalAlignment.Center;
		Button save = UI.Button("Save QR Code", delegate
		{
		}, primary: true);
		Button copy = UI.Button("Copy Image", delegate
		{
		});
		int generation = 0;
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(60L)
		};
		timer.Tick += delegate
		{
			timer.Stop();
			Refresh();
		};
		TextBox textBox = new TextBox
		{
			Text = model.Text,
			Height = 70.0,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Padding = new Thickness(4.0)
		};
		textBox.TextChanged += delegate
		{
			model.Text = textBox.Text;
			Changed();
		};
		StackPanel wifiPanel = new StackPanel
		{
			Visibility = Visibility.Collapsed
		};
		TextBox network = new TextBox
		{
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			Width = 170.0
		};
		TextBox password = new TextBox
		{
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			Width = 170.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0)
		};
		network.TextChanged += delegate
		{
			model.Network = network.Text;
			Changed();
		};
		password.TextChanged += delegate
		{
			model.Password = password.Text;
			Changed();
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		stackPanel2.Children.Add(Labeled("Network name", network));
		StackPanel passwordField = Labeled("Password", password);
		stackPanel2.Children.Add(passwordField);
		wifiPanel.Children.Add(stackPanel2);
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
			LastChildFill = false
		};
		ComboBox element = UI.Picker((from s in Enum.GetValues<QrContent.Security>()
			select (s: s, QrContent.Title(s))).ToList(), model.Security, delegate(QrContent.Security s)
		{
			model.Security = s;
			passwordField.Visibility = ((s == QrContent.Security.None) ? Visibility.Collapsed : Visibility.Visible);
			Changed();
		});
		DockPanel.SetDock(element, Dock.Left);
		dockPanel.Children.Add(element);
		CheckBox element2 = UI.Check("Hidden network", value: false, delegate(bool v)
		{
			model.Hidden = v;
			Changed();
		});
		DockPanel.SetDock(element2, Dock.Right);
		dockPanel.Children.Add(element2);
		wifiPanel.Children.Add(dockPanel);
		Border border = UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(false, "Text or Link"),
			(true, "Wi-Fi")
		}), selected: false, delegate(bool wifi)
		{
			model.Wifi = wifi;
			textBox.Visibility = (wifi ? Visibility.Collapsed : Visibility.Visible);
			wifiPanel.Visibility = ((!wifi) ? Visibility.Collapsed : Visibility.Visible);
			Changed();
		});
		border.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
		stackPanel.Children.Add(border);
		stackPanel.Children.Add(textBox);
		stackPanel.Children.Add(wifiPanel);
		stackPanel.Children.Add(UI.Divider());
		QrMatrix matrix = new QrMatrix(5, "1101110110011011101010111".Select((char c) => c == '1').ToArray());
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		List<Border> list = new List<Border>();
		QrDesign.ModuleShape[] values = Enum.GetValues<QrDesign.ModuleShape>();
		foreach (QrDesign.ModuleShape shape in values)
		{
			string text2 = new QrDesign
			{
				Module = shape,
				Margin = 0,
				Background = null
			}.ModulePath(matrix);
			Geometry geometry = Geometry.Parse("F1 " + text2);
			stackPanel3.Children.Add(Swatch(geometry, 5.0, QrDesign.Title(shape), shape == model.Module, delegate
			{
				model.Module = shape;
			}, list));
		}
		stackPanel.Children.Add(UI.Row("Shape", stackPanel3, 40.0));
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		List<Border> list2 = new List<Border>();
		QrDesign.EyeShape[] values2 = Enum.GetValues<QrDesign.EyeShape>();
		foreach (QrDesign.EyeShape shape2 in values2)
		{
			PathBuilder pathBuilder = new PathBuilder();
			QrDesign.AddEye(shape2, new Rect(0.0, 0.0, 7.0, 7.0), pathBuilder);
			stackPanel4.Children.Add(Swatch(Geometry.Parse("F0 " + pathBuilder), 7.0, QrDesign.Title(shape2), shape2 == model.Eye, delegate
			{
				model.Eye = shape2;
			}, list2));
		}
		stackPanel.Children.Add(UI.Row("Corners", stackPanel4, 40.0));
		stackPanel.Children.Add(UI.Divider());
		StackPanel stackPanel5 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		Border gradientWell = ColorWell.Make(model.GradientEnd, delegate(Color c)
		{
			model.GradientEnd = c;
			Changed();
		});
		gradientWell.Visibility = Visibility.Collapsed;
		TextBlock arrow = UI.Icon((Theme.Direction == FlowDirection.RightToLeft) ? "\ue72a" : "\ue72b", 9.0, Theme.Muted).Pad(6.0, 0.0);
		arrow.Visibility = Visibility.Collapsed;
		stackPanel5.Children.Add(ColorWell.Make(model.Foreground, delegate(Color c)
		{
			model.Foreground = c;
			Changed();
		}));
		stackPanel5.Children.Add(arrow);
		stackPanel5.Children.Add(gradientWell);
		stackPanel.Children.Add(UI.Row("Code color", stackPanel5, 34.0));
		DockPanel dockPanel2 = new DockPanel
		{
			LastChildFill = false
		};
		Border eyeWell = ColorWell.Make(model.EyeColor, delegate(Color c)
		{
			model.EyeColor = c;
			Changed();
		});
		eyeWell.Visibility = Visibility.Collapsed;
		CheckBox element3 = UI.Check("Gradient", value: false, delegate(bool v)
		{
			model.Gradient = v;
			Border border4 = gradientWell;
			Visibility visibility = (arrow.Visibility = ((!v) ? Visibility.Collapsed : Visibility.Visible));
			border4.Visibility = visibility;
			Changed();
		});
		CheckBox checkBox = UI.Check("Own corner color", value: false, delegate(bool v)
		{
			model.SeparateEye = v;
			eyeWell.Visibility = ((!v) ? Visibility.Collapsed : Visibility.Visible);
			Changed();
		});
		checkBox.Margin = new Thickness(14.0, 0.0, 0.0, 0.0);
		DockPanel.SetDock(element3, Dock.Left);
		DockPanel.SetDock(checkBox, Dock.Left);
		DockPanel.SetDock(eyeWell, Dock.Right);
		dockPanel2.Children.Add(element3);
		dockPanel2.Children.Add(checkBox);
		dockPanel2.Children.Add(eyeWell);
		stackPanel.Children.Add(dockPanel2);
		StackPanel stackPanel6 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		Border backgroundWell = ColorWell.Make(model.Background, delegate(Color c)
		{
			model.Background = c;
			Changed();
		});
		stackPanel6.Children.Add(UI.Check("Transparent", value: false, delegate(bool v)
		{
			model.Transparent = v;
			backgroundWell.IsEnabled = !v;
			backgroundWell.Opacity = (v ? 0.4 : 1.0);
			Changed();
		}).Pad(0.0, 0.0));
		stackPanel6.Children.Add(new Border
		{
			Width = 10.0
		});
		stackPanel6.Children.Add(backgroundWell);
		stackPanel.Children.Add(UI.Row("Background", stackPanel6, 34.0));
		stackPanel.Children.Add(UI.Divider());
		ComboBox correctionPicker = null;
		StackPanel stackPanel7 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		Image logoThumb = new Image
		{
			Width = 24.0,
			Height = 24.0,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
			Visibility = Visibility.Collapsed
		};
		StackPanel logoDetails = new StackPanel
		{
			Visibility = Visibility.Collapsed
		};
		TextBlock logoHint = UI.Caption("Or drop an image on the preview.");
		Button removeLogo = UI.Button("Remove", delegate
		{
		});
		removeLogo.Visibility = Visibility.Collapsed;
		Button chooseLogo = UI.Button("Choose Logo…", delegate
		{
		});
		chooseLogo.Click += delegate
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = L10n.T("Choose a logo for the middle of the code"),
				Filter = "Images|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp;*.svg;*.heic;*.tif;*.tiff"
			};
			if (openFileDialog.ShowDialog(window) == true)
			{
				LoadLogo(openFileDialog.FileName);
			}
		};
		removeLogo.Click += delegate
		{
			SetLogo(null);
		};
		removeLogo.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		stackPanel7.Children.Add(logoThumb);
		stackPanel7.Children.Add(removeLogo);
		stackPanel7.Children.Add(chooseLogo);
		stackPanel.Children.Add(UI.Row("Logo", stackPanel7, 34.0));
		logoDetails.Children.Add(UI.Row("Logo size", UI.Slider(0.12, 0.32, model.LogoScale, delegate(double v)
		{
			model.LogoScale = v;
			Changed();
		}, (double v) => L10n.T($"{Math.Round(v * 100.0)}%"), null, 140.0)));
		Border border2 = UI.Segmented((from p in Enum.GetValues<QrDesign.Plate>()
			select (p: p, QrDesign.Title(p))).ToList(), model.Plate, delegate(QrDesign.Plate p)
		{
			model.Plate = p;
			Changed();
		});
		border2.Width = 230.0;
		logoDetails.Children.Add(UI.Row("Behind the logo", border2, 34.0));
		stackPanel.Children.Add(logoDetails);
		stackPanel.Children.Add(logoHint);
		stackPanel.Children.Add(UI.Divider());
		stackPanel.Children.Add(UI.Row("Margin", UI.Slider(0.0, 8.0, model.Margin, delegate(double v)
		{
			model.Margin = (int)Math.Round(v);
			Changed();
		}, (double v) => L10n.T($"{Math.Round(v)}"), null, 140.0, 1.0)));
		correctionPicker = UI.Picker((from c in Enum.GetValues<QrMatrix.Correction>()
			select (c: c, QrMatrix.Title(c))).ToList(), model.Correction, delegate(QrMatrix.Correction c)
		{
			model.Correction = c;
			Changed();
		});
		stackPanel.Children.Add(UI.Row("Error correction", correctionPicker));
		StackPanel stackPanel8 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		ComboBox pixels = UI.Picker(new global::_003C_003Ez__ReadOnlyArray<(int, string)>(new(int, string)[4]
		{
			(512, "512 px"),
			(1024, "1024 px"),
			(2048, "2048 px"),
			(4096, "4096 px")
		}), model.Pixels, delegate(int p)
		{
			model.Pixels = p;
		});
		pixels.MinWidth = 90.0;
		pixels.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		stackPanel8.Children.Add(pixels);
		Border border3 = UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(false, "PNG"),
			(true, "SVG")
		}), selected: false, delegate(bool svg)
		{
			model.Svg = svg;
			pixels.Visibility = (svg ? Visibility.Collapsed : Visibility.Visible);
		});
		border3.Width = 110.0;
		stackPanel8.Children.Add(border3);
		stackPanel.Children.Add(UI.Row("Format", stackPanel8, 34.0));
		previewBox.DragOver += delegate(object _, DragEventArgs e)
		{
			e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
			e.Handled = true;
		};
		previewBox.DragEnter += delegate
		{
			previewBox.BorderBrush = Theme.Accent;
		};
		previewBox.DragLeave += delegate
		{
			previewBox.BorderBrush = Brushes.Transparent;
		};
		previewBox.Drop += delegate(object _, DragEventArgs e)
		{
			previewBox.BorderBrush = Brushes.Transparent;
			if (e.Data.GetData(DataFormats.FileDrop) is string[] array && array.Length > 0)
			{
				LoadLogo(array[0]);
			}
		};
		StackPanel stackPanel9 = new StackPanel
		{
			Margin = new Thickness(20.0, 0.0, 0.0, 0.0)
		};
		stackPanel9.Children.Add(previewBox);
		stackPanel9.Children.Add(status.Pad(0.0, 10.0));
		stackPanel9.Children.Add(modules);
		StackPanel stackPanel10 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		stackPanel10.Children.Add(stackPanel);
		stackPanel10.Children.Add(stackPanel9);
		window.Body = stackPanel10;
		copy.Click += delegate
		{
			QrMatrix matrix2 = model.Matrix;
			if (matrix2 != null)
			{
				System.Windows.Clipboard.SetImage(model.Design.Image(matrix2, model.Pixels));
				Hud.Shared.ShowMessage(L10n.T("QR code copied"), Hud.Style.Success);
				Sounds.Done();
			}
		};
		save.Click += delegate
		{
			QrMatrix matrix2 = model.Matrix;
			if (matrix2 != null)
			{
				QrDesign design = model.Design;
				string text3 = (model.Svg ? "svg" : "png");
				string output = ((source != null) ? Output.PathFor(source, text3, "QR") : Output.Unique(Path.Combine((Settings.Current.OutputLocation == OutputLocation.Desktop) ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) : Output.Downloads, "QR Code." + text3)));
				byte[] bytes = (model.Svg ? Encoding.UTF8.GetBytes(design.Svg(matrix2)) : QrDesign.Png(design.Image(matrix2, model.Pixels)));
				window.Close();
				IReadOnlyList<string> files2;
				if (source == null)
				{
					IReadOnlyList<string> readOnlyList = Array.Empty<string>();
					files2 = readOnlyList;
				}
				else
				{
					IReadOnlyList<string> readOnlyList = new global::_003C_003Ez__ReadOnlySingleElementList<string>(source);
					files2 = readOnlyList;
				}
				Runner.Run("Create QR Code", files2, async delegate
				{
					await File.WriteAllBytesAsync(output, bytes);
					return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
				});
			}
		};
		save.IsDefault = true;
		window.Leading(copy);
		window.Trailing(save);
		Refresh();
		return window;
		void Changed()
		{
			timer.Stop();
			timer.Start();
		}
		static StackPanel Labeled(string title, Control box)
		{
			StackPanel stackPanel11 = new StackPanel();
			UIElementCollection children = stackPanel11.Children;
			Brush muted2 = Theme.Muted;
			children.Add(UI.Text(title, 11.0, null, muted2));
			stackPanel11.Children.Add(box);
			return stackPanel11;
		}
		void LoadLogo(string path)
		{
			try
			{
				using MagickImage magickImage = ImageEngine.Load(path);
				if (Math.Max(magickImage.Width, magickImage.Height) > 1024)
				{
					magickImage.Resize(new MagickGeometry(1024u, 1024u));
				}
				SetLogo(Pictures.ToBitmap(magickImage));
			}
			catch (Exception)
			{
				Hud.Shared.ShowMessage(L10n.T("Could not read " + Path.GetFileName(path)), Hud.Style.Failure);
			}
		}
		static void Paint(Border b, bool on)
		{
			b.Background = new SolidColorBrush(Color.FromArgb((byte)(on ? 26u : 10u), 0, 0, 0));
			b.BorderBrush = (on ? Theme.Accent : Brushes.Transparent);
		}
		void Refresh()
		{
			generation++;
			int current = generation;
			string problem = model.Problem;
			Button button = save;
			bool isEnabled = (copy.IsEnabled = problem == null);
			button.IsEnabled = isEnabled;
			previewBox.Background = (model.Transparent ? Brushes.White : Theme.Card);
			if (problem == null)
			{
				QrMatrix matrix2 = model.Matrix;
				if (matrix2 != null)
				{
					BitmapSource bitmapSource = model.Design.Image(matrix2, 560);
					preview.Source = bitmapSource;
					placeholder.Visibility = Visibility.Collapsed;
					modules.Text = L10n.T($"{matrix2.Size} × {matrix2.Size} modules");
					status.Text = L10n.T("Checking…");
					status.Foreground = Theme.Muted;
					byte[] png = QrDesign.Png(bitmapSource);
					string payload = model.Payload;
					Task.Run(delegate
					{
						using MagickImage magickImage = new MagickImage(png);
						magickImage.BackgroundColor = MagickColors.White;
						magickImage.Alpha(AlphaAction.Remove);
						IReadOnlyList<string> found = Recognition.Codes(magickImage);
						window.Dispatcher.BeginInvoke((Action)delegate
						{
							if (current == generation)
							{
								bool flag2 = found.Contains(payload);
								status.Text = L10n.T(flag2 ? "Scans correctly" : "May not scan. Use a smaller logo, more contrast or higher correction.");
								status.Foreground = (flag2 ? new SolidColorBrush(Color.FromRgb(31, 157, 85)) : new SolidColorBrush(Color.FromRgb(194, 106, 0)));
							}
						});
					});
					return;
				}
			}
			preview.Source = null;
			placeholder.Visibility = Visibility.Visible;
			status.Text = L10n.T(problem ?? "");
			status.Foreground = Theme.Muted;
			modules.Text = "";
		}
		void SetLogo(BitmapSource? logo)
		{
			if (logo != null && model.Logo == null)
			{
				model.Correction = QrMatrix.Correction.High;
				correctionPicker.SelectedIndex = 3;
			}
			model.Logo = logo;
			logoThumb.Source = logo;
			bool flag = logo != null;
			Image image = logoThumb;
			Button button = removeLogo;
			Visibility visibility = (logoDetails.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible));
			Visibility visibility3 = (button.Visibility = visibility);
			image.Visibility = visibility3;
			logoHint.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			UIElementCollection children = ((StackPanel)chooseLogo.Content).Children;
			((TextBlock)children[children.Count - 1]).Text = L10n.T(flag ? "Change…" : "Choose Logo…");
			Changed();
		}
		Border Swatch(Geometry geometry2, double units, string title, bool selected, Action pick, List<Border> group)
		{
			DrawingGroup drawing = new DrawingGroup
			{
				Children = 
				{
					(Drawing)new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0.0, 0.0, units, units))),
					(Drawing)new GeometryDrawing(Theme.Ink, null, geometry2)
				}
			};
			Image child = new Image
			{
				Source = new DrawingImage(drawing),
				Width = 22.0,
				Height = 22.0
			};
			Border border4 = new Border
			{
				Width = 34.0,
				Height = 34.0,
				CornerRadius = new CornerRadius(7.0),
				Margin = new Thickness(0.0, 0.0, 4.0, 0.0),
				Cursor = Cursors.Hand,
				ToolTip = L10n.T(title),
				Child = child,
				BorderThickness = new Thickness(2.0)
			};
			Paint(border4, selected);
			border4.MouseLeftButtonUp += delegate
			{
				foreach (Border item in group)
				{
					Paint(item, item == border4);
				}
				pick();
				Changed();
			};
			group.Add(border4);
			return border4;
		}
	}
}
