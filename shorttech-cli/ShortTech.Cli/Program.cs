using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ShortTech.Core;
using ShortTech.Core.Documents;
using ShortTech.Core.Images;
using Microsoft.Web.WebView2.Core;

namespace ShortTech.Cli;

public static class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		List<string> list = args.ToList();
		int num = list.IndexOf("--lang");
		if (num >= 0 && num + 1 < list.Count)
		{
			L10n.Override(list[num + 1]);
			list.RemoveRange(num, 2);
		}
		if (list.Count == 0)
		{
			return Usage();
		}
		try
		{
			int result;
			switch (list[0].TrimStart('-'))
			{
			case "list":
				result = List(list.Skip(1).Select(Path.GetFullPath).ToList());
				break;
			case "run":
				if (list.Count >= 3)
				{
					result = Run(list[1], list.Skip(2).Select(Path.GetFullPath).ToList()).GetAwaiter().GetResult();
					break;
				}
				goto default;
			case "doctor":
				result = Doctor().GetAwaiter().GetResult();
				break;
			default:
				result = Usage();
				break;
			}
			return result;
		}
		catch (ShortTechException ex)
		{
			Console.Error.WriteLine("error: " + ex.Message);
			return 1;
		}
	}

	private static int Usage()
	{
		Console.Error.WriteLine("usage: shorttech-cli list <files…> | shorttech-cli run <operation-id> <files…> | shorttech-cli doctor   [--lang ar|en]");
		return 2;
	}

	private static int List(IReadOnlyList<string> files)
	{
		WheelMode[] values = Enum.GetValues<WheelMode>();
		foreach (WheelMode wheelMode in values)
		{
			Console.WriteLine($"{wheelMode}: {string.Join(" ", from op in Catalog.For(wheelMode, files)
				select op.Id)}");
		}
		return 0;
	}

	private static async Task<int> Run(string id, IReadOnlyList<string> files)
	{
		ShortTechOperation shortTechOperation = Catalog.Find(id, files);
		if (shortTechOperation == null)
		{
			IEnumerable<string> values = from op in Enum.GetValues<WheelMode>().SelectMany((WheelMode mode) => Catalog.For(mode, files))
				select op.Id;
			Console.Error.WriteLine("No operation " + id + " for these files. Available: " + string.Join(" ", values));
			return 2;
		}
		string lastLine = "";
		OperationContext context = new OperationContext(delegate(string text)
		{
			if (!(text == lastLine))
			{
				lastLine = text;
				Console.Error.WriteLine(text);
			}
		});
		IReadOnlyList<string> obj = await shortTechOperation.Run(files, context);
		if (context.Message != null)
		{
			Console.Error.WriteLine(L10n.T(context.Message));
		}
		foreach (string item in obj)
		{
			Console.WriteLine(item);
		}
		return 0;
	}

	private static async Task<int> Doctor()
	{
		Console.WriteLine($"Short Tech {typeof(Catalog).Assembly.GetName().Version} · {Environment.OSVersion.VersionString} · {RuntimeInformation.OSArchitecture}");
		Console.WriteLine($"Language: {(L10n.IsArabic ? "ar" : "en")} · .NET {Environment.Version}");
		string ffmpeg = ProcessRunner.Find("ffmpeg");
		string text = ProcessRunner.Find("ffprobe");
		Console.WriteLine("ffmpeg: " + (ffmpeg ?? "not found"));
		Console.WriteLine("ffprobe: " + (text ?? "not found"));
		if (ffmpeg != null)
		{
			Console.WriteLine("  " + (await ProcessRunner.Run(ffmpeg, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-hide_banner", "-version" }))).Output.Split('\n').FirstOrDefault()?.Trim());
			ProcessRunner.Result encoders = await ProcessRunner.Run(ffmpeg, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-hide_banner", "-encoders" }));
			string[] source = new string[8] { "libx264", "libx265", "libvpx-vp9", "libopus", "libmp3lame", "libvorbis", "aac", "flac" };
			Console.WriteLine("  encoders: " + string.Join(" ", source.Select((string e) => (!encoders.Output.Contains(" " + e + " ")) ? ("(" + e + " missing)") : e)));
		}
		IEnumerable<string> values = from f in ImageFormat.All
			where f.CanWrite
			select f.Name;
		Console.WriteLine("Image output: " + string.Join(", ", values));
		Console.WriteLine($"Office: Word {(Office.HasWord ? "yes" : "no")} · PowerPoint {(Office.HasPowerPoint ? "yes" : "no")} · LibreOffice {((Office.LibreOffice != null) ? "yes" : "no")}");
		Console.WriteLine("WebView2: " + (WebViewVersion() ?? "not found"));
		return 0;
	}

	private static string? WebViewVersion()
	{
		try
		{
			return CoreWebView2Environment.GetAvailableBrowserVersionString();
		}
		catch (Exception)
		{
			return null;
		}
	}
}
