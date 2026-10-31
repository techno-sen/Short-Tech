using System;
using Microsoft.Win32;

namespace ShortTech.App;

public static class Startup
{
	private const string Key = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	public static bool IsEnabled => Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")?.GetValue("Short Tech") != null;

	public static void Set(bool enabled)
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
		if (enabled)
		{
			registryKey.SetValue("Short Tech", "\"" + Environment.ProcessPath + "\"");
		}
		else
		{
			registryKey.DeleteValue("Short Tech", throwOnMissingValue: false);
		}
	}
}
