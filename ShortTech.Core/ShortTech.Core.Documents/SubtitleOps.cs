using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShortTech.Core.Documents;

public static partial class SubtitleOps
{
	private sealed record Cue(double Start, double End, string Text);

	public static readonly IReadOnlyList<string> Targets = new global::_003C_003Ez__ReadOnlyArray<string>(new string[3] { "srt", "vtt", "txt" });

	public static string Title(string target)
	{
		return target.ToUpperInvariant();
	}

	[GeneratedRegex("(?:(\\d+):)?(\\d{1,2}):(\\d{2})[.,](\\d{1,3})\\s*-->\\s*(?:(\\d+):)?(\\d{1,2}):(\\d{2})[.,](\\d{1,3})")]
	private static partial Regex TimingLine();

	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, string target, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			List<Cue> list = Parse(DocumentOps.ReadText(file));
			if (list.Count == 0)
			{
				throw new ShortTechException("No valid subtitle cues were found in " + Path.GetFileName(file) + ".");
			}
			string output = Output.PathFor(file, target);
			string path = output;
			string contents = ((target == "srt") ? Srt(list) : ((!(target == "vtt")) ? (string.Join("\n", list.Select((Cue c) => c.Text)) + "\n") : Vtt(list)));
			await File.WriteAllTextAsync(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			outputs.Add(output);
		}
		return outputs;
	}

	private static List<Cue> Parse(string text)
	{
		List<Cue> list = new List<Cue>();
		string[] array = Regex.Split(text.Replace("\r\n", "\n").Replace('\r', '\n'), "\\n\\s*\\n");
		for (int i = 0; i < array.Length; i++)
		{
			List<string> list2 = array[i].Split('\n').ToList();
			int num = list2.FindIndex((string line) => TimingLine().IsMatch(line));
			Match match;
			if (num >= 0)
			{
				match = TimingLine().Match(list2[num]);
				string text2 = string.Join("\n", list2.Skip(num + 1)).Trim();
				if (text2.Length > 0)
				{
					list.Add(new Cue(Time(1), Time(5), text2));
				}
			}
			double Time(int group)
			{
				return (double)((match.Groups[group].Success ? (int.Parse(match.Groups[group].Value) * 3600) : 0) + int.Parse(match.Groups[group + 1].Value) * 60 + int.Parse(match.Groups[group + 2].Value)) + (double)int.Parse(match.Groups[group + 3].Value.PadRight(3, '0')) / 1000.0;
			}
		}
		return list;
	}

	private static string Stamp(double seconds, char separator)
	{
		long num = (long)Math.Round(seconds * 1000.0);
		return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}{3}{4:000}", num / 3600000, num / 60000 % 60, num / 1000 % 60, separator, num % 1000);
	}

	private static string Srt(List<Cue> cues)
	{
		return string.Concat(cues.Select((Cue cue, int i) => $"{i + 1}\n{Stamp(cue.Start, ',')} --> {Stamp(cue.End, ',')}\n{Regex.Replace(cue.Text, "<[^>]+>", "")}\n\n"));
	}

	private static string Vtt(List<Cue> cues)
	{
		return string.Concat("WEBVTT\n\n", string.Concat(cues.Select((Cue cue) => $"{Stamp(cue.Start, '.')} --> {Stamp(cue.End, '.')}\n{cue.Text}\n\n")));
	}
}
