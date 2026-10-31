using System;
using System.IO;

namespace ShortTech.Core;

public static class Output
{
	public static string Downloads => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

	public static string Directory(string source)
	{
		switch (Settings.Current.OutputLocation)
		{
		case OutputLocation.Downloads:
			return Downloads;
		case OutputLocation.Desktop:
			return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		default:
		{
			string text = Path.GetDirectoryName(Path.GetFullPath(source)) ?? Downloads;
			if (!IsWritable(text))
			{
				return Downloads;
			}
			return text;
		}
		}
	}

	public static string PathFor(string source, string extension, string? suffix = null)
	{
		string text = Path.GetFileNameWithoutExtension(source);
		if (text.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text;
			text = text2.Substring(0, text2.Length - 4);
		}
		if (suffix != null)
		{
			text = text + " " + suffix;
		}
		return Unique(Path.Combine(Directory(source), text + "." + extension));
	}

	public static string Named(string name, string source)
	{
		return Unique(Path.Combine(Directory(source), name));
	}

	public static string Unique(string path)
	{
		if (!File.Exists(path) && !System.IO.Directory.Exists(path))
		{
			return path;
		}
		string directoryName = Path.GetDirectoryName(path);
		string extension = Path.GetExtension(path);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		int num = 2;
		string text;
		while (true)
		{
			text = Path.Combine(directoryName, $"{fileNameWithoutExtension} {num}{extension}");
			if (!File.Exists(text) && !System.IO.Directory.Exists(text))
			{
				break;
			}
			num++;
		}
		return text;
	}

	public static TempFolder Temp()
	{
		return new TempFolder();
	}

	private static bool IsWritable(string folder)
	{
		try
		{
			string path = Path.Combine(folder, $".shorttech-{Guid.NewGuid():N}");
			File.WriteAllText(path, "");
			File.Delete(path);
			return true;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return false;
		}
	}
}
