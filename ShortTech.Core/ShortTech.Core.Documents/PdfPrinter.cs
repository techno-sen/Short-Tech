using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace ShortTech.Core.Documents;

public static class PdfPrinter
{
	private static readonly string UserData = Path.Combine(Settings.Folder, "WebView2");

	public static Task Print(string html, string output, bool landscape = false)
	{
		TaskCompletionSource done = new TaskCompletionSource();
		Thread thread = new Thread((ThreadStart)delegate
		{
			Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
			dispatcher.InvokeAsync((Func<Task>)async delegate
			{
				try
				{
					await PrintOnThisThread(html, output, landscape);
					done.SetResult();
				}
				catch (Exception ex)
				{
					done.SetException((ex is WebView2RuntimeNotFoundException) ? new ShortTechException("Microsoft Edge WebView2 is needed to make PDFs. Install it from microsoft.com/edge/webview2.") : ex);
				}
				finally
				{
					dispatcher.InvokeShutdown();
				}
			});
			Dispatcher.Run();
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		return done.Task;
	}

	private static async Task PrintOnThisThread(string html, string output, bool landscape)
	{
		HwndSource window = new HwndSource(new HwndSourceParameters("ShortTechPdf")
		{
			Width = 1,
			Height = 1,
			WindowStyle = 0,
			ParentWindow = new IntPtr(-3)
		});
		try
		{
			Directory.CreateDirectory(UserData);
			CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, UserData);
			CoreWebView2Controller controller = await environment.CreateCoreWebView2ControllerAsync(window.Handle);
			CoreWebView2 web = controller.CoreWebView2;
			web.Settings.IsScriptEnabled = false;
			TaskCompletionSource<bool> loaded = new TaskCompletionSource<bool>();
			web.NavigationCompleted += delegate(object? _, CoreWebView2NavigationCompletedEventArgs e)
			{
				loaded.TrySetResult(e.IsSuccess);
			};
			using TempFolder temp = Output.Temp();
			string page = temp.File("page.html");
			await File.WriteAllTextAsync(page, html);
			web.Navigate(new Uri(page).AbsoluteUri);
			if (!(await loaded.Task))
			{
				throw new ShortTechException("The document could not be printed as PDF");
			}
			CoreWebView2PrintSettings coreWebView2PrintSettings = environment.CreatePrintSettings();
			coreWebView2PrintSettings.Orientation = (landscape ? CoreWebView2PrintOrientation.Landscape : CoreWebView2PrintOrientation.Portrait);
			coreWebView2PrintSettings.ShouldPrintBackgrounds = true;
			coreWebView2PrintSettings.ShouldPrintHeaderAndFooter = false;
			double num = (coreWebView2PrintSettings.MarginRight = 0.0);
			double num3 = (coreWebView2PrintSettings.MarginLeft = num);
			double marginTop = (coreWebView2PrintSettings.MarginBottom = num3);
			coreWebView2PrintSettings.MarginTop = marginTop;
			bool num6 = await web.PrintToPdfAsync(output, coreWebView2PrintSettings);
			controller.Close();
			if (!num6 || !File.Exists(output))
			{
				throw new ShortTechException("The document could not be printed as PDF");
			}
		}
		finally
		{
			window.Dispose();
		}
	}
}
