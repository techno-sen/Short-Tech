using System;
using System.Globalization;

namespace ShortTech.Core;

public static class Format
{
	public static string Bytes(long bytes)
	{
		string[] array = new string[5] { "bytes", "KB", "MB", "GB", "TB" };
		double num = bytes;
		int num2 = 0;
		while (num >= 1000.0 && num2 < array.Length - 1)
		{
			num /= 1000.0;
			num2++;
		}
		if (num2 != 0)
		{
			return num.ToString((num < 10.0) ? "0.#" : "0", CultureInfo.InvariantCulture) + " " + array[num2];
		}
		return $"{bytes} {array[0]}";
	}

	public static string Time(double seconds)
	{
		seconds = Math.Max(0.0, seconds);
		int num = (int)seconds / 60;
		return $"{num}:{seconds - (double)(num * 60):00.00}".Replace(',', '.');
	}

	public static double? ParseTime(string text)
	{
		double num = 0.0;
		string[] array = text.Trim().Split(':');
		for (int i = 0; i < array.Length; i++)
		{
			if (!double.TryParse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return null;
			}
			num = num * 60.0 + result;
		}
		return num;
	}
}
