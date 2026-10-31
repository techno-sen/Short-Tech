using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ShortTech.Core;

namespace ShortTech.App;

public static class Runner
{
	public static void Start(ShortTechOperation operation, IReadOnlyList<string> files)
	{
		string window = operation.Window;
		if (window == null || !ToolWindows.Open(window, files))
		{
			Run(operation, files);
		}
	}

	public static void Run(string title, IReadOnlyList<string> files, OperationWork work, bool producesFiles = true)
	{
		Run(new ShortTechOperation("window", title, "", work)
		{
			ProducesFiles = producesFiles
		}, files);
	}

	public static async void Run(ShortTechOperation operation, IReadOnlyList<string> files)
	{
		Hud hud = Hud.Shared;
		hud.ShowMessage(operation.Title + "…", Hud.Style.Working);
		Dispatcher dispatcher = Application.Current.Dispatcher;
		OperationContext context = new OperationContext(delegate(string text3)
		{
			dispatcher.BeginInvoke((Action)delegate
			{
				hud.Update(text3);
			});
		}, delegate(double fraction)
		{
			dispatcher.BeginInvoke((Action)delegate
			{
				hud.SetProgress(fraction);
			});
		});
		try
		{
			IReadOnlyList<string> readOnlyList = await Task.Run(() => operation.Run(files, context));
			string text = context.Message;
			if (text == null)
			{
				text = readOnlyList.Count switch
				{
					0 => operation.Title + " done", 
					1 => "Saved " + Path.GetFileName(readOnlyList[0]), 
					_ => $"Saved {readOnlyList.Count} files", 
				};
			}
			string text2 = text;
			hud.ShowMessage(text2, Hud.Style.Success);
			Sounds.Done();
			if (operation.ProducesFiles && readOnlyList.Count > 0 && Settings.Current.ShowResultsInFloat)
			{
				Floats.ShowResults(readOnlyList);
			}
		}
		catch (Exception ex)
		{
			Log.Write($"{operation.Id} failed: {ex}");
			hud.ShowMessage((ex is ShortTechException) ? ex.Message : (L10n.T("Something went wrong: ") + ex.Message), Hud.Style.Failure);
			Sounds.Failed();
		}
	}
}
