using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShortTech.Core;

public static partial class FileActions
{
	[LibraryImport("shlwapi.dll", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int StrCmpLogicalW(string a, string b);

	public static IReadOnlyList<string> NameOrder(IReadOnlyList<string> files)
	{
		List<string> list = files.Select((string f) => f.TrimEnd('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		list.Sort((string a, string b) => StrCmpLogicalW(Path.GetFileName(a), Path.GetFileName(b)));
		return list;
	}

	public static string? RenameProblem(string baseName)
	{
		string text = baseName.Trim();
		if (text.Length == 0)
		{
			return "Enter a name.";
		}
		if (text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || text.EndsWith('.'))
		{
			return "The name contains characters Windows does not allow.";
		}
		return null;
	}

	public static IReadOnlyList<string> RenameTargets(IReadOnlyList<string> files, string baseName, int start)
	{
		string text = baseName.Trim();
		int totalWidth = (start + files.Count - 1).ToString(CultureInfo.InvariantCulture).Length;
		List<string> list = new List<string>();
		for (int i = 0; i < files.Count; i++)
		{
			string path = files[i];
			string extension = Directory.Exists(path) ? "" : Path.GetExtension(path);
			string stem = (files.Count == 1) ? text : (text + " " + (start + i).ToString(CultureInfo.InvariantCulture).PadLeft(totalWidth, '0'));
			list.Add(Path.Combine(Path.GetDirectoryName(path) ?? "", stem + extension));
		}
		return list;
	}

	public static async Task<IReadOnlyList<string>> Rename(IReadOnlyList<string> files, string baseName, int start, OperationContext context)
	{
		string? problem = RenameProblem(baseName);
		if (problem != null)
		{
			throw new ShortTechException(problem);
		}
		IReadOnlyList<string> sources = NameOrder(files);
		IReadOnlyList<string> targets = RenameTargets(sources, baseName, start);
		List<string> results = await Task.Run(() => MoveAll(sources, targets));
		context.Finish((results.Count == 1) ? "Renamed 1 item" : $"Renamed {results.Count} items");
		return results;
	}

	private static List<string> MoveAll(IReadOnlyList<string> sources, IReadOnlyList<string> targets)
	{
		List<(string Original, string Current)> moved = new List<(string, string)>();
		try
		{
			foreach (string source in sources)
			{
				string temp = Path.Combine(Path.GetDirectoryName(source) ?? "", $".shorttech-rename-{Guid.NewGuid():N}");
				MovePath(source, temp);
				moved.Add((source, temp));
			}
			List<string> results = new List<string>();
			for (int i = 0; i < moved.Count; i++)
			{
				string target = Output.Unique(targets[i]);
				MovePath(moved[i].Current, target);
				moved[i] = (moved[i].Original, target);
				results.Add(target);
			}
			return results;
		}
		catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
		{
			foreach (var (original, current) in moved)
			{
				if (current == original || File.Exists(original) || Directory.Exists(original))
				{
					continue;
				}
				try
				{
					MovePath(current, original);
				}
				catch (Exception ex2) when (ex2 is IOException || ex2 is UnauthorizedAccessException)
				{
				}
			}
			throw new ShortTechException("Could not rename the files: " + ex.Message);
		}
	}

	private static void MovePath(string from, string to)
	{
		if (Directory.Exists(from))
		{
			Directory.Move(from, to);
		}
		else
		{
			File.Move(from, to);
		}
	}

	public static Func<IReadOnlyList<string>, Task>? ShareHandler { get; set; }

	public static Func<IReadOnlyList<string>, Task>? FloatHandler { get; set; }

	public static Func<IReadOnlyList<string>, Task<string?>>? ChooseFolder { get; set; }

	public static async Task<IReadOnlyList<string>> Share(IReadOnlyList<string> files, OperationContext context)
	{
		if (ShareHandler == null)
		{
			throw new ShortTechException("This tool needs its window.");
		}
		await ShareHandler(files);
		context.Finish("Sharing");
		return Array.Empty<string>();
	}

	public static async Task<IReadOnlyList<string>> NewFloat(IReadOnlyList<string> files, OperationContext context)
	{
		if (FloatHandler == null)
		{
			throw new ShortTechException("This tool needs its window.");
		}
		await FloatHandler(files);
		context.Finish("Added to a new Float");
		return Array.Empty<string>();
	}

	public static Task<IReadOnlyList<string>> CopyFiles(IReadOnlyList<string> files, OperationContext context)
	{
		Clipboard.CopyFiles(files);
		context.Finish((files.Count == 1) ? "File copied" : $"{files.Count} files copied");
		return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
	}

	public static Task<IReadOnlyList<string>> CopyPaths(IReadOnlyList<string> files, OperationContext context)
	{
		Clipboard.CopyText(string.Join(Environment.NewLine, files));
		context.Finish((files.Count == 1) ? "Path copied" : $"{files.Count} paths copied");
		return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
	}

	public static async Task<IReadOnlyList<string>> Move(IReadOnlyList<string> files, OperationContext context)
	{
		if (ChooseFolder == null)
		{
			throw new ShortTechException("This tool needs its window.");
		}
		string text = await ChooseFolder(files);
		if (text == null)
		{
			context.Finish("Move cancelled");
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		foreach (string file in files)
		{
			string text2 = Output.Unique(Path.Combine(text, Path.GetFileName(file.TrimEnd('\\', '/'))));
			if (Directory.Exists(file))
			{
				Directory.Move(file, text2);
			}
			else
			{
				File.Move(file, text2);
			}
			list.Add(text2);
		}
		context.Finish((files.Count == 1) ? ("Moved 1 item to " + Path.GetFileName(text)) : $"Moved {files.Count} items to {Path.GetFileName(text)}");
		return list;
	}

	public static Task<IReadOnlyList<string>> OpenTerminal(IReadOnlyList<string> files, OperationContext context)
	{
		string text = files[0];
		string text2 = (Directory.Exists(text) ? text : (Path.GetDirectoryName(text) ?? text));
		string text3 = ProcessRunner.Find("wt");
		Process.Start((text3 != null) ? new ProcessStartInfo(text3)
		{
			ArgumentList = { "-d", text2 },
			UseShellExecute = false
		} : new ProcessStartInfo("powershell.exe")
		{
			ArgumentList = 
			{
				"-NoExit",
				"-Command",
				"Set-Location -LiteralPath '" + text2.Replace("'", "''") + "'"
			},
			UseShellExecute = true
		});
		context.Finish("Opened Terminal in " + Path.GetFileName(text2));
		return Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
	}

	public static void ShowInExplorer(string path)
	{
		Process.Start(new ProcessStartInfo("explorer.exe")
		{
			ArgumentList = { "/select,", path },
			UseShellExecute = false
		});
	}
}
