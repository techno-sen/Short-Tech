using System;
using System.Collections.Generic;
using System.Windows;
using ShortTech.App.Windows;
using ShortTech.Core;

namespace ShortTech.App;

public static class ToolWindows
{
	private static readonly Dictionary<string, Func<IReadOnlyList<string>, Window?>> Makers = new Dictionary<string, Func<IReadOnlyList<string>, Window>>();

	private static bool _registered;

	public static IEnumerable<string> Names => Makers.Keys;

	public static void Register(string name, Func<IReadOnlyList<string>, Window?> maker)
	{
		Makers[name] = maker;
	}

	public static bool Open(string name, IReadOnlyList<string> files)
	{
		EnsureRegistered();
		if (!Makers.TryGetValue(name, out Func<IReadOnlyList<string>, Window> value))
		{
			return false;
		}
		try
		{
			Window window = value(files);
			if (window == null)
			{
				return true;
			}
			if (window is ToolWindow toolWindow)
			{
				toolWindow.Present();
			}
			else
			{
				window.Show();
				window.Activate();
			}
			return true;
		}
		catch (ShortTechException ex)
		{
			Hud.Shared.ShowMessage(ex.Message, Hud.Style.Failure);
			return true;
		}
	}

	public static Window? Make(string name, IReadOnlyList<string> files)
	{
		EnsureRegistered();
		if (!Makers.TryGetValue(name, out Func<IReadOnlyList<string>, Window> value))
		{
			return null;
		}
		return value(files);
	}

	private static void EnsureRegistered()
	{
		if (!_registered)
		{
			_registered = true;
			ShortTech.App.Windows.Catalog.RegisterAll();
		}
	}
}
