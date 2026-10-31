using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShortTech.Core;

public static class L10n
{
	private static readonly Dictionary<string, string> Strings = Load("strings.ar.json");

	private static readonly List<(Regex Pattern, string Template)> Patterns = (from pair in Load("patterns.ar.json")
		select (new Regex("^" + string.Join("(.+?)", Regex.Split(pair.Key, "\\{\\d\\}").Select(Regex.Escape)) + "$", RegexOptions.Singleline | RegexOptions.CultureInvariant), Value: pair.Value) into pair
		orderby pair.Item1.ToString().Length descending
		select pair).ToList();

	public static bool IsArabic { get; private set; } = Settings.Current.Language == "ar";

	public static bool IsRightToLeft => IsArabic;

	public static void Override(string language)
	{
		IsArabic = language == "ar";
	}

	public static string T(string english)
	{
		if (!IsArabic || string.IsNullOrEmpty(english))
		{
			return english;
		}
		if (Strings.TryGetValue(english, out string value))
		{
			return value;
		}
		string[] array = new string[2] { "…", ":" };
		foreach (string text in array)
		{
			if (english.EndsWith(text))
			{
				if (Strings.TryGetValue(english.Substring(0, english.Length - 1), out string value2))
				{
					return value2 + text;
				}
			}
		}
		foreach (var pattern in Patterns)
		{
			Regex item = pattern.Pattern;
			string item2 = pattern.Template;
			Match match = item.Match(english);
			if (match.Success)
			{
				string text2 = item2;
				for (int num = match.Groups.Count - 1; num >= 1; num--)
				{
					text2 = text2.Replace("{" + num + "}", T(match.Groups[num].Value));
				}
				return text2;
			}
		}
		return english;
	}

	public static string Upper(string text)
	{
		if (!IsArabic)
		{
			return text.ToUpper(CultureInfo.CurrentCulture);
		}
		return text;
	}

	private static Dictionary<string, string> Load(string name)
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
		if (stream == null)
		{
			return new Dictionary<string, string>();
		}
		return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
	}
}
