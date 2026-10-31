using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.App.Wheel;
using ShortTech.Core;

namespace ShortTech.App;

public static class Snapshots
{
	public static int Run(Application app, string[] args)
	{
		int code = 1;
		app.Startup += delegate
		{
			try
			{
				code = Render(args);
			}
			catch (Exception value)
			{
				Console.Error.WriteLine(value);
				code = 1;
			}
			app.Shutdown(code);
		};
		app.Run();
		return code;
	}

	private static int Render(string[] args)
	{
		if (args.Length < 3)
		{
			return 2;
		}
		FrameworkElement frameworkElement = null;
		Window window = null;
		if (args[0] == "--snapshot-window")
		{
			string text = args[1];
			List<string> files = args.Skip(3).Select(Path.GetFullPath).ToList();
			Window window2;
			switch (text)
			{
			case "welcome0":
			case "welcome1":
			case "welcome2":
			case "welcome3":
				window2 = WelcomeWindow.Make(text[text.Length - 1] - 48);
				break;
			case "settings":
				window2 = SettingsWindow.Make();
				break;
			default:
				window2 = ToolWindows.Make(text, files);
				break;
			}
			window = window2;
			if (window == null)
			{
				Console.Error.WriteLine("No window " + text + ". Known: " + string.Join(" ", ToolWindows.Names));
				return 2;
			}
		}
		else if (args[0] == "--snapshot-wheel")
		{
			WheelMode mode = Enum.Parse<WheelMode>(args[1], ignoreCase: true);
			int result;
			List<string> files2 = (from a in args.Skip(3)
				where !int.TryParse(a, out result)
				select a).Select(Path.GetFullPath).ToList();
			int? hovered = (from a in args.Skip(3)
				select (!int.TryParse(a, out result)) ? ((int?)null) : new int?(result)).FirstOrDefault((int? i) => i.HasValue);
			frameworkElement = WheelPreview.Make(mode, files2, hovered);
		}
		if (window != null)
		{
			window.ShowActivated = false;
			window.Left = -20000.0;
			window.Show();
			frameworkElement = (FrameworkElement)window.Content;
			Pump(TimeSpan.FromSeconds(2.5));
		}
		if (frameworkElement == null)
		{
			return 2;
		}
		if (window == null)
		{
			frameworkElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			frameworkElement.Arrange(new Rect(frameworkElement.DesiredSize));
		}
		frameworkElement.UpdateLayout();
		int num = (int)Math.Ceiling(window?.ActualWidth ?? frameworkElement.ActualWidth);
		int num2 = (int)Math.Ceiling(window?.ActualHeight ?? frameworkElement.ActualHeight);
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(num * 2, num2 * 2, 192.0, 192.0, PixelFormats.Pbgra32);
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			drawingContext.DrawRectangle(window?.Background ?? Brushes.Transparent, null, new Rect(0.0, 0.0, num, num2));
			int num3;
			if (window == null)
			{
				num3 = 0;
			}
			else
			{
				num3 = ((window.FlowDirection == FlowDirection.RightToLeft) ? 1 : 0);
				if (num3 != 0)
				{
					drawingContext.PushTransform(new ScaleTransform(-1.0, 1.0, (double)num / 2.0, 0.0));
				}
			}
			drawingContext.DrawRectangle(new VisualBrush(frameworkElement), null, new Rect(0.0, 0.0, num, num2));
			if (num3 != 0)
			{
				drawingContext.Pop();
			}
		}
		renderTargetBitmap.Render(drawingVisual);
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
		using (FileStream stream = File.Create(args[2]))
		{
			pngBitmapEncoder.Save(stream);
		}
		Console.WriteLine(Path.GetFullPath(args[2]));
		window?.Close();
		return 0;
	}

	public static void Pump(TimeSpan time)
	{
		DispatcherFrame frame = new DispatcherFrame();
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = time
		};
		timer.Tick += delegate
		{
			timer.Stop();
			frame.Continue = false;
		};
		timer.Start();
		Dispatcher.PushFrame(frame);
	}
}
