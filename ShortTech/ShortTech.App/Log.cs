using System;
using System.IO;
using ShortTech.Core;

namespace ShortTech.App;

public static class Log
{
	private static readonly string FilePath = Path.Combine(Settings.Folder, "shorttech.log");

	private static readonly object Gate = new object();

	public static void Write(string text)
	{
		try
		{
			lock (Gate)
			{
				Directory.CreateDirectory(Settings.Folder);
				if (File.Exists(FilePath) && new FileInfo(FilePath).Length > 2000000)
				{
					File.Delete(FilePath);
				}
				File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}{Environment.NewLine}");
			}
		}
		catch (IOException)
		{
		}
	}
}
