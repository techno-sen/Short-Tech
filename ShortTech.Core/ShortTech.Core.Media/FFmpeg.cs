using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShortTech.Core.Media;

public static class FFmpeg
{
	private static readonly string[] Prefix = new string[8] { "-y", "-hide_banner", "-nostdin", "-loglevel", "error", "-progress", "pipe:1", "-nostats" };

	private static HashSet<string>? _encoders;

	public static string Aac => "aac";

	public static HashSet<string> Encoders
	{
		get
		{
			if (_encoders != null)
			{
				return _encoders;
			}
			HashSet<string> hashSet = new HashSet<string>();
			string text = ProcessRunner.Find("ffmpeg");
			if (text != null)
			{
				string[] array = ProcessRunner.Run(text, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-hide_banner", "-encoders" })).GetAwaiter().GetResult()
					.Output.Split('\n');
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length >= 2 && array2[0].Length == 6)
					{
						hashSet.Add(array2[1]);
					}
				}
			}
			return _encoders = hashSet;
		}
	}

	public static string[] H264 => new string[8]
	{
		"-c:v",
		"libx264",
		"-preset",
		"medium",
		"-crf",
		Crf(hevc: false).ToString(),
		"-pix_fmt",
		"yuv420p"
	};

	public static string[] Hevc10 => new string[12]
	{
		"-c:v",
		"libx265",
		"-preset",
		"medium",
		"-crf",
		Crf(hevc: true).ToString(),
		"-pix_fmt",
		"yuv420p10le",
		"-tag:v",
		"hvc1",
		"-x265-params",
		"log-level=error"
	};

	public static string[] WebMVideo(int crf = 33)
	{
		if (Encoders.Contains("libvpx-vp9"))
		{
			return new string[12]
			{
				"-c:v",
				"libvpx-vp9",
				"-crf",
				crf.ToString(),
				"-b:v",
				"0",
				"-row-mt",
				"1",
				"-deadline",
				"good",
				"-cpu-used",
				"4"
			};
		}
		return new string[8]
		{
			"-c:v",
			"libsvtav1",
			"-crf",
			(crf + 2).ToString(),
			"-preset",
			"8",
			"-pix_fmt",
			"yuv420p"
		};
	}

	public static int Crf(bool hevc)
	{
		return Settings.Current.VideoQuality switch
		{
			VideoQuality.High => hevc ? 20 : 19, 
			VideoQuality.Balanced => hevc ? 24 : 22, 
			_ => hevc ? 28 : 26, 
		};
	}

	public static Task Run(string input, string output, IEnumerable<string> arguments, double duration, Action<double>? progress, IEnumerable<string>? inputOptions = null, CancellationToken cancellation = default(CancellationToken))
	{
		List<string> list = new List<string>();
		list.AddRange(inputOptions ?? Array.Empty<string>());
		list.Add("-i");
		list.Add(input);
		list.AddRange(arguments);
		list.Add(output);
		return Run(new _003C_003Ez__ReadOnlyList<string>(list), duration, progress, cancellation);
	}

	public static async Task Run(IEnumerable<string> arguments, double duration, Action<double>? progress, CancellationToken cancellation = default(CancellationToken))
	{
		string tool = ProcessRunner.Require("ffmpeg");
		List<string> list = new List<string>();
		list.AddRange(Prefix);
		list.AddRange(arguments);
		ProcessRunner.Result result = await ProcessRunner.Run(tool, new _003C_003Ez__ReadOnlyList<string>(list), delegate(string line)
		{
			if (duration > 0.0 && line.StartsWith("out_time_us=") && double.TryParse(line.Substring(12), CultureInfo.InvariantCulture, out var result2))
			{
				progress?.Invoke(Math.Clamp(result2 / 1000000.0 / duration, 0.0, 1.0));
			}
		}, cancellation);
		if (result.ExitCode != 0)
		{
			string text = result.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? $"exit code {result.ExitCode}";
			throw new ShortTechException("ffmpeg: " + text);
		}
	}

	public static async Task<byte[]> Capture(IEnumerable<string> arguments, CancellationToken cancellation = default(CancellationToken))
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(ProcessRunner.Require("ffmpeg"))
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		List<string> list = new List<string>();
		list.Add("-hide_banner");
		list.Add("-nostdin");
		list.Add("-loglevel");
		list.Add("error");
		list.AddRange(arguments);
		string[] array = list.ToArray();
		foreach (string item in array)
		{
			processStartInfo.ArgumentList.Add(item);
		}
		using Process process = Process.Start(processStartInfo);
		process.StandardError.ReadToEndAsync();
		using MemoryStream memory = new MemoryStream();
		await process.StandardOutput.BaseStream.CopyToAsync(memory, cancellation);
		await process.WaitForExitAsync(cancellation);
		return memory.ToArray();
	}

	public static async Task<string> Report(IEnumerable<string> arguments)
	{
		string tool = ProcessRunner.Require("ffmpeg");
		List<string> list = new List<string>();
		list.Add("-hide_banner");
		list.Add("-nostdin");
		list.AddRange(arguments);
		return (await ProcessRunner.Run(tool, new _003C_003Ez__ReadOnlyList<string>(list))).Error;
	}

	public static string Seconds(double value)
	{
		return value.ToString("0.000", CultureInfo.InvariantCulture);
	}
}
