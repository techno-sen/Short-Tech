using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShortTech.Core.Media;

public static class MediaProbe
{
	public static async Task<MediaInfo> Info(string path)
	{
		long size = (File.Exists(path) ? new FileInfo(path).Length : 0);
		string text = ProcessRunner.Find("ffprobe");
		if (text == null)
		{
			return await FromReport(path, size);
		}
		ProcessRunner.Result result = await ProcessRunner.Run(text, new global::_003C_003Ez__ReadOnlyArray<string>(new string[7] { "-v", "error", "-print_format", "json", "-show_format", "-show_streams", path }));
		if (result.ExitCode != 0)
		{
			return new MediaInfo
			{
				FileSize = size
			};
		}
		using JsonDocument jsonDocument = JsonDocument.Parse(result.Output);
		JsonElement rootElement = jsonDocument.RootElement;
		double result2 = 0.0;
		if (rootElement.TryGetProperty("format", out var value) && value.TryGetProperty("duration", out var value2))
		{
			double.TryParse(value2.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result2);
		}
		string text2 = null;
		string text3 = null;
		int num = 0;
		int num2 = 0;
		int audioBits = 0;
		int audioChannels = 0;
		double frameRate = 0.0;
		bool isHighDepth = false;
		if (rootElement.TryGetProperty("streams", out var value3))
		{
			foreach (JsonElement item in value3.EnumerateArray())
			{
				string text4 = item.GetProperty("codec_type").GetString();
				JsonElement value4;
				string text5 = (item.TryGetProperty("codec_name", out value4) ? value4.GetString() : null);
				JsonElement value5;
				JsonElement value6;
				bool flag = item.TryGetProperty("disposition", out value5) && value5.TryGetProperty("attached_pic", out value6) && value6.GetInt32() == 1;
				if (text4 == "video" && text2 == null && !flag)
				{
					text2 = text5;
					num = (item.TryGetProperty("width", out var value7) ? value7.GetInt32() : 0);
					num2 = (item.TryGetProperty("height", out var value8) ? value8.GetInt32() : 0);
					JsonElement value9;
					string text6 = (item.TryGetProperty("pix_fmt", out value9) ? (value9.GetString() ?? "") : "");
					JsonElement value10;
					string text7 = (item.TryGetProperty("color_transfer", out value10) ? (value10.GetString() ?? "") : "");
					bool flag2 = text6.Contains("10") || text6.Contains("12");
					if (!flag2)
					{
						bool flag3 = ((text7 == "smpte2084" || text7 == "arib-std-b67") ? true : false);
						flag2 = flag3;
					}
					isHighDepth = flag2 || text5 == "prores";
					if (item.TryGetProperty("avg_frame_rate", out var value11))
					{
						frameRate = Fraction(value11.GetString());
					}
					switch (Rotation(item))
					{
					case -270:
					case -90:
					case 90:
					case 270:
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					if (flag2)
					{
						int num3 = num2;
						num2 = num;
						num = num3;
					}
					if (result2 <= 0.0 && item.TryGetProperty("duration", out var value12))
					{
						double.TryParse(value12.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result2);
					}
				}
				else if (text4 == "audio" && text3 == null)
				{
					text3 = ((text5 != null && text5.StartsWith("pcm_")) ? "pcm" : text5);
					JsonElement value13;
					int result4;
					if (text5 != null && text5.StartsWith("pcm_"))
					{
						audioBits = (int.TryParse(new string(text5.Where(char.IsDigit).ToArray()), out var result3) ? result3 : 16);
					}
					else if (item.TryGetProperty("bits_per_raw_sample", out value13) && int.TryParse(value13.GetString(), out result4))
					{
						audioBits = result4;
					}
					audioChannels = (item.TryGetProperty("channels", out var value14) ? value14.GetInt32() : 2);
				}
			}
		}
		return new MediaInfo
		{
			Duration = result2,
			VideoCodec = text2,
			AudioCodec = text3,
			AudioBits = audioBits,
			AudioChannels = audioChannels,
			IsHighDepth = isHighDepth,
			Width = num,
			Height = num2,
			FrameRate = frameRate,
			FileSize = size
		};
	}

	private static async Task<MediaInfo> FromReport(string path, long size)
	{
		if (ProcessRunner.Find("ffmpeg") == null)
		{
			return new MediaInfo
			{
				FileSize = size
			};
		}
		string obj = await FFmpeg.Report(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-i", path }));
		double duration = 0.0;
		Match match = Regex.Match(obj, "Duration: (\\d+):(\\d+):(\\d+(?:\\.\\d+)?)");
		if (match.Success)
		{
			duration = (double)(int.Parse(match.Groups[1].Value) * 3600 + int.Parse(match.Groups[2].Value) * 60) + double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
		}
		string text = null;
		string text2 = null;
		int num = 0;
		int num2 = 0;
		int audioBits = 0;
		int audioChannels = 0;
		int num3 = 0;
		double frameRate = 0.0;
		bool isHighDepth = false;
		Match match2 = Regex.Match(obj, "(?:displaymatrix: rotation of|rotate\\s*:)\\s*(-?\\d+(?:\\.\\d+)?)");
		if (match2.Success)
		{
			num3 = (int)Math.Round(double.Parse(match2.Groups[1].Value, CultureInfo.InvariantCulture));
		}
		foreach (string item in from l in obj.Split('\n')
			where l.Contains("Stream #")
			select l)
		{
			if (text == null)
			{
				Match match3 = Regex.Match(item, "Video: (\\w+)");
				if (match3 != null && match3.Success && !item.Contains("attached pic"))
				{
					text = match3.Groups[1].Value;
					isHighDepth = item.Contains("p10") || item.Contains("p12") || item.Contains("arib-std-b67") || item.Contains("smpte2084") || text == "prores";
					Match match4 = Regex.Match(item, ", (\\d{2,5})x(\\d{2,5})");
					if (match4 != null && match4.Success)
					{
						num = int.Parse(match4.Groups[1].Value);
						num2 = int.Parse(match4.Groups[2].Value);
					}
					Match match5 = Regex.Match(item, ", (\\d+(?:\\.\\d+)?) fps");
					if (match5 != null && match5.Success)
					{
						frameRate = double.Parse(match5.Groups[1].Value, CultureInfo.InvariantCulture);
					}
					continue;
				}
			}
			if (text2 != null)
			{
				continue;
			}
			Match match6 = Regex.Match(item, "Audio: (\\w+)");
			if (match6 != null && match6.Success)
			{
				string text3 = match6.Groups[1].Value;
				if (text3.StartsWith("pcm_"))
				{
					audioBits = (int.TryParse(new string(text3.Where(char.IsDigit).ToArray()), out var result) ? result : 16);
					text3 = "pcm";
				}
				else if (item.Contains("s32") || item.Contains("(24 bit)"))
				{
					audioBits = 24;
				}
				text2 = text3;
				audioChannels = (item.Contains("mono") ? 1 : (item.Contains("5.1") ? 6 : 2));
			}
		}
		bool flag;
		switch (num3)
		{
		case -270:
		case -90:
		case 90:
		case 270:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			int num4 = num2;
			num2 = num;
			num = num4;
		}
		return new MediaInfo
		{
			Duration = duration,
			VideoCodec = text,
			AudioCodec = text2,
			AudioBits = audioBits,
			AudioChannels = audioChannels,
			IsHighDepth = isHighDepth,
			Width = num,
			Height = num2,
			FrameRate = frameRate,
			FileSize = size
		};
	}

	private static int Rotation(JsonElement stream)
	{
		if (stream.TryGetProperty("side_data_list", out var value))
		{
			foreach (JsonElement item in value.EnumerateArray())
			{
				if (item.TryGetProperty("rotation", out var value2))
				{
					return value2.GetInt32();
				}
			}
		}
		if (stream.TryGetProperty("tags", out var value3) && value3.TryGetProperty("rotate", out var value4) && int.TryParse(value4.GetString(), out var result))
		{
			return result;
		}
		return 0;
	}

	private static double Fraction(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0.0;
		}
		string[] array = text.Split('/');
		if (array.Length == 2 && double.TryParse(array[0], CultureInfo.InvariantCulture, out var result) && double.TryParse(array[1], CultureInfo.InvariantCulture, out var result2) && result2 > 0.0)
		{
			return result / result2;
		}
		if (!double.TryParse(text, CultureInfo.InvariantCulture, out var result3))
		{
			return 0.0;
		}
		return result3;
	}
}
