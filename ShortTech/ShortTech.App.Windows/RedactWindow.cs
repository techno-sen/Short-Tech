using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Images;
using ImageMagick;

namespace ShortTech.App.Windows;

public static class RedactWindow
{
	public static Window Make(IReadOnlyList<string> files)
	{
		string file = files[0];
		MagickImage item = Pictures.LoadPreview(file).Preview;
		ToolWindow window = new ToolWindow("Redact Photo", 640.0);
		BoxEditor editor = new BoxEditor(400.0)
		{
			Source = Pictures.ToBitmap(item)
		};
		bool blur = false;
		Rgb color = new Rgb(0.0, 0.0, 0.0);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(editor);
		TextBlock caption = UI.Caption("Draw rectangles over the areas to hide. Drag a box to move it, or its corners to resize.");
		stackPanel.Children.Add(caption.Pad(0.0, 8.0));
		stackPanel.Children.Add(UI.Row("Style", UI.Segmented(new global::_003C_003Ez__ReadOnlyArray<(bool, string)>(new(bool, string)[2]
		{
			(false, "Solid"),
			(true, "Blur")
		}), selected: false, delegate(bool v)
		{
			blur = v;
			editor.Fill = (v ? new SolidColorBrush(Color.FromArgb(153, 128, 128, 128)) : new SolidColorBrush(Color.FromArgb(238, 0, 0, 0)));
			caption.Text = L10n.T(v ? "Blur softens details. Use Solid for sensitive information." : "Draw rectangles over the areas to hide. Drag a box to move it, or its corners to resize.");
			editor.Draw();
		})));
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal
		};
		stackPanel2.Children.Add(UI.Button("Faces", delegate
		{
			Detect(faces: true);
		}, primary: false, "\ue77b"));
		stackPanel2.Children.Add(UI.Button("Text", delegate
		{
			Detect(faces: false);
		}, primary: false, "\ue8d2").Pad(6.0, 0.0));
		stackPanel.Children.Add(UI.Row("Detect", stackPanel2));
		window.Body = stackPanel;
		window.Leading(UI.Button("Delete Redaction", editor.DeleteSelected));
		window.Trailing(UI.Button("Save Redacted Copy", delegate
		{
			List<ImageRedaction.Box> boxes = editor.Boxes.Select((BoxEditor.Box b) => new ImageRedaction.Box(b.Rect.X, b.Rect.Y, b.Rect.Width, b.Rect.Height)).ToList();
			if (boxes.Count == 0)
			{
				Hud.Shared.ShowMessage("Draw at least one redaction.", Hud.Style.Failure);
			}
			else
			{
				window.Close();
				Runner.Run("Redact Photo", new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await ImageRedaction.Save(f[0], boxes, blur, color, c)));
			}
		}, primary: true));
		return window;
		async void Detect(bool faces)
		{
			Hud.Shared.ShowMessage(faces ? "Detecting faces…" : "Detecting text…", Hud.Style.Working);
			try
			{
				using MagickImage image = await ImageEngine.LoadAsync(file, 2400);
				IReadOnlyList<(double, double, double, double)> readOnlyList = ((!faces) ? (await Recognition.TextBoxes(image)) : (await Recognition.Faces(image)));
				IReadOnlyList<(double, double, double, double)> readOnlyList2 = readOnlyList;
				foreach (var (num, num2, num3, num4) in readOnlyList2)
				{
					editor.Add(new Rect(num / (double)image.Width, num2 / (double)image.Height, num3 / (double)image.Width, num4 / (double)image.Height));
				}
				Hud.Shared.ShowMessage((readOnlyList2.Count != 0) ? (faces ? $"Covered {readOnlyList2.Count} face{((readOnlyList2.Count == 1) ? "" : "s")}" : $"Covered {readOnlyList2.Count} text area{((readOnlyList2.Count == 1) ? "" : "s")}") : (faces ? "No faces found" : "No text found"), Hud.Style.Success);
			}
			catch (Exception ex)
			{
				Hud.Shared.ShowMessage(ex.Message, Hud.Style.Failure);
			}
		}
	}
}
