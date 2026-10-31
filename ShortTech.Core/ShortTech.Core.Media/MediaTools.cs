using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShortTech.Core.Images;

namespace ShortTech.Core.Media;

public static class MediaTools
{
	private static string N(double value)
	{
		return value.ToString("0.####", CultureInfo.InvariantCulture);
	}

	private static string[] VideoEncoder(MediaInfo info)
	{
		if (!info.IsHighDepth)
		{
			return FFmpeg.H264;
		}
		return FFmpeg.Hevc10;
	}

	private static string VideoExtension(string file)
	{
		bool flag;
		switch (FileKinds.Ext(file))
		{
		case "mov":
		case "mp4":
		case "m4v":
		case "mkv":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return "mp4";
		}
		return FileKinds.Ext(file);
	}

	private static string[] FastStart(string ext)
	{
		bool flag;
		switch (ext)
		{
		case "mp4":
		case "mov":
		case "m4v":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return Array.Empty<string>();
		}
		return new string[2] { "-movflags", "+faststart" };
	}

	public static async Task<string> ChangeSpeed(string file, double speed, OperationContext context)
	{
		MediaInfo mediaInfo = await MediaProbe.Info(file);
		speed = Math.Clamp(speed, 0.25, 4.0);
		List<string> list = new List<string>();
		double num;
		for (num = speed; num > 2.0; num /= 2.0)
		{
			list.Add("atempo=2");
		}
		while (num < 0.5)
		{
			list.Add("atempo=0.5");
			num /= 0.5;
		}
		list.Add("atempo=" + N(num));
		MediaInfo info = mediaInfo with
		{
			Duration = mediaInfo.Duration / speed
		};
		string suffix = ((speed == Math.Round(speed)) ? $"{(int)speed}x" : (speed.ToString("0.##", CultureInfo.InvariantCulture) + "x"));
		if (mediaInfo.HasVideo)
		{
			string text = VideoExtension(file);
			string output = Output.PathFor(file, text, suffix);
			List<string> list2 = new List<string>
			{
				"-map",
				"0:v:0",
				"-map",
				"0:a?",
				"-map_metadata",
				"0",
				"-filter:v",
				"setpts=PTS/" + N(speed)
			};
			list2.AddRange(VideoEncoder(mediaInfo));
			if (mediaInfo.HasAudio)
			{
				list2.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[6]
				{
					"-filter:a",
					string.Join(",", list),
					"-c:a",
					FFmpeg.Aac,
					"-b:a",
					"192k"
				}));
			}
			list2.AddRange(FastStart(text));
			await MediaOps.Encode(file, output, list2, info, context.ProgressFor(0, 1), null, context);
			return output;
		}
		string audio = Output.PathFor(file, FileKinds.Ext(file), suffix);
		List<string> list3 = new List<string>();
		list3.Add("-vn");
		list3.Add("-map");
		list3.Add("0:a:0");
		list3.Add("-map_metadata");
		list3.Add("0");
		list3.Add("-filter:a");
		list3.Add(string.Join(",", list));
		list3.AddRange(MediaOps.AudioCodec(FileKinds.Ext(file), mediaInfo.AudioBits >= 24));
		await MediaOps.Encode(file, audio, new _003C_003Ez__ReadOnlyList<string>(list3), info, context.ProgressFor(0, 1), null, context);
		return audio;
	}

	public static async Task<IReadOnlyList<string>> Crop(IReadOnlyList<string> files, double x, double y, double w, double h, OperationContext context)
	{
		string crop = $"crop=w=trunc(iw*{N(w)}/2)*2:h=trunc(ih*{N(h)}/2)*2:x=trunc(iw*{N(x)}):y=trunc(ih*{N(y)})";
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			MediaInfo info = await MediaProbe.Info(file);
			string text = VideoExtension(file);
			string output = Output.PathFor(file, text, "cropped");
			List<string> list = new List<string>();
			list.Add("-map");
			list.Add("0:v:0");
			list.Add("-map");
			list.Add("0:a?");
			list.Add("-map_metadata");
			list.Add("0");
			list.Add("-vf");
			list.Add(crop);
			list.AddRange(VideoEncoder(info));
			list.Add("-c:a");
			list.Add("copy");
			list.AddRange(FastStart(text));
			await MediaOps.Encode(file, output, new _003C_003Ez__ReadOnlyList<string>(list), info, context.ProgressFor(index, files.Count), null, context);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Split(string file, IReadOnlyList<double> points, ISet<int> removed, bool joinKept, OperationContext context)
	{
		MediaInfo info = await MediaProbe.Info(file);
		List<double> bounds = new List<double> { 0.0 };
		bounds.AddRange(from p in points
			orderby p
			where p > 0.05 && p < info.Duration - 0.05
			select p);
		bounds.Add(info.Duration);
		List<(double Start, double End)> segments = (from i in Enumerable.Range(0, bounds.Count - 1)
			where !removed.Contains(i)
			select (Start: bounds[i], End: bounds[i + 1])).ToList();
		if (segments.Count == 0)
		{
			throw new ShortTechException("Keep at least one clip.");
		}
		string stem = Path.GetFileNameWithoutExtension(file);
		string ext = FileKinds.Ext(file);
		if (joinKept)
		{
			using (TempFolder staging = Output.Temp())
			{
				List<string> parts = new List<string>();
				for (int order = 0; order < segments.Count; order++)
				{
					context.Report($"Cutting clip {order + 1} of {segments.Count}");
					string part = staging.File($"part {order}.{ext}");
					await MediaOps.CutSegment(file, segments[order].Start, segments[order].End, part, info, context.ProgressFor(order, segments.Count + 1), context);
					parts.Add(part);
				}
				context.Report($"Joining {parts.Count} clips");
				string output = Output.PathFor(file, VideoExtension(file), "edited");
				if (parts.Count == 1)
				{
					File.Move(parts[0], output);
				}
				else
				{
					File.Move((await MediaOps.Join(parts, context))[0], output);
				}
				return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
			}
		}
		string folder = Output.Named(stem + " clips", file);
		Directory.CreateDirectory(folder);
		for (int order = 0; order < segments.Count; order++)
		{
			context.Report($"Saving clip {order + 1} of {segments.Count}");
			string output2 = Path.Combine(folder, $"{stem} {order + 1:00}.{ext}");
			await MediaOps.CutSegment(file, segments[order].Start, segments[order].End, output2, info, context.ProgressFor(order, segments.Count), context);
		}
		context.Finish((segments.Count == 1) ? "Saved 1 clip" : $"Saved {segments.Count} clips");
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(folder);
	}

	public static async Task<IReadOnlyList<string>> SaveFrames(string file, IReadOnlyList<double> times, OperationContext context)
	{
		if (times.Count == 0)
		{
			throw new ShortTechException("Add a frame to save.");
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
		string folder = ((times.Count > 1) ? Output.Named(fileNameWithoutExtension + " frames", file) : null);
		if (folder != null)
		{
			Directory.CreateDirectory(folder);
		}
		List<string> outputs = new List<string>();
		List<double> sorted = times.OrderBy((double t) => t).ToList();
		for (int index = 0; index < sorted.Count; index++)
		{
			context.Progress((double)index / (double)sorted.Count);
			double time = sorted[index];
			string output = ((folder != null) ? Path.Combine(folder, $"Frame {index + 1:000} - {time.ToString("0.000", CultureInfo.InvariantCulture)}s.png") : Output.PathFor(file, "png", time.ToString("0.000", CultureInfo.InvariantCulture) + "s"));
			await MediaOps.Frame(file, time, output);
			if (folder == null)
			{
				outputs.Add(output);
			}
		}
		return (folder != null) ? ((IReadOnlyList<string>)new List<string>(1) { folder }) : ((IReadOnlyList<string>)outputs);
	}

	public static async Task<string> Redact(string file, IReadOnlyList<VideoRedaction> redactions, bool blur, Rgb color, OperationContext context)
	{
		if (redactions.Count == 0)
		{
			throw new ShortTechException("Draw at least one redaction.");
		}
		MediaInfo mediaInfo = await MediaProbe.Info(file);
		string text = (mediaInfo.IsHighDepth ? "yuv420p10le" : "yuv420p");
		string item;
		if (blur)
		{
			StringBuilder stringBuilder = new StringBuilder("[0:v]null[v0];");
			StringBuilder stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler;
			for (int i = 0; i < redactions.Count; i++)
			{
				VideoRedaction videoRedaction = redactions[i];
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(47, 8, stringBuilder2);
				handler.AppendLiteral("[v");
				handler.AppendFormatted(i);
				handler.AppendLiteral("]split[b");
				handler.AppendFormatted(i);
				handler.AppendLiteral("][c");
				handler.AppendFormatted(i);
				handler.AppendLiteral("];[c");
				handler.AppendFormatted(i);
				handler.AppendLiteral("]crop=w=iw*");
				handler.AppendFormatted(N(videoRedaction.W));
				handler.AppendLiteral(":h=ih*");
				handler.AppendFormatted(N(videoRedaction.H));
				handler.AppendLiteral(":x=iw*");
				handler.AppendFormatted(N(videoRedaction.X));
				handler.AppendLiteral(":y=ih*");
				handler.AppendFormatted(N(videoRedaction.Y));
				handler.AppendLiteral(",");
				stringBuilder3.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(102, 1, stringBuilder2);
				handler.AppendLiteral("scale=w=max(2\\,trunc(iw/16)):h=max(2\\,trunc(ih/16)),scale=w=iw*16:h=ih*16:flags=neighbor,boxblur=4[p");
				handler.AppendFormatted(i);
				handler.AppendLiteral("];");
				stringBuilder4.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(47, 6, stringBuilder2);
				handler.AppendLiteral("[b");
				handler.AppendFormatted(i);
				handler.AppendLiteral("][p");
				handler.AppendFormatted(i);
				handler.AppendLiteral("]overlay=x=main_w*");
				handler.AppendFormatted(N(videoRedaction.X));
				handler.AppendLiteral(":y=main_h*");
				handler.AppendFormatted(N(videoRedaction.Y));
				handler.AppendLiteral(":enable='");
				handler.AppendFormatted(Enable(videoRedaction));
				handler.AppendLiteral("'[v");
				handler.AppendFormatted(i + 1);
				handler.AppendLiteral("];");
				stringBuilder5.Append(ref handler);
			}
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
			handler.AppendLiteral("[v");
			handler.AppendFormatted(redactions.Count);
			handler.AppendLiteral("]format=");
			handler.AppendFormatted(text);
			handler.AppendLiteral("[out]");
			stringBuilder6.Append(ref handler);
			item = stringBuilder.ToString();
		}
		else
		{
			string hex = $"0x{(int)(color.R * 255.0):X2}{(int)(color.G * 255.0):X2}{(int)(color.B * 255.0):X2}";
			item = "[0:v]" + string.Join(",", redactions.Select((VideoRedaction r) => $"drawbox=x=iw*{N(r.X)}:y=ih*{N(r.Y)}:w=iw*{N(r.W)}:h=ih*{N(r.H)}:color={hex}@1:t=fill:enable='{Enable(r)}'")) + ",format=" + text + "[out]";
		}
		string text2 = VideoExtension(file);
		string output = Output.PathFor(file, text2, "redacted");
		List<string> list = new List<string>();
		list.Add("-filter_complex");
		list.Add(item);
		list.Add("-map");
		list.Add("[out]");
		list.Add("-map");
		list.Add("0:a?");
		list.Add("-map_metadata");
		list.Add("-1");
		list.AddRange(VideoEncoder(mediaInfo));
		list.Add("-c:a");
		list.Add("copy");
		list.AddRange(FastStart(text2));
		await MediaOps.Encode(file, output, new _003C_003Ez__ReadOnlyList<string>(list), mediaInfo, context.ProgressFor(0, 1), null, context);
		return output;
		static string Enable(VideoRedaction r)
		{
			return $"between(t\\,{N(r.Start)}\\,{N(r.End)})";
		}
	}

	public static async Task<string> Visualize(string file, string? image, VisualizerShape shape, Rgb color, OperationContext context)
	{
		MediaInfo mediaInfo = await MediaProbe.Info(file);
		if (!mediaInfo.HasAudio)
		{
			throw new ShortTechException("This file has no playable audio.");
		}
		(int Width, int Height) tuple = shape.Size();
		int item = tuple.Width;
		int item2 = tuple.Height;
		string output = Output.PathFor(file, "mp4", "visualizer");
		string value = $"0x{(int)(color.R * 255.0):X2}{(int)(color.G * 255.0):X2}{(int)(color.B * 255.0):X2}";
		List<string> list = ((image != null) ? new List<string>(14)
		{
			"-loop",
			"1",
			"-framerate",
			"15",
			"-i",
			image,
			"-i",
			file,
			"-filter_complex",
			$"[0:v]scale={item}:{item2}:force_original_aspect_ratio=decrease:force_divisible_by=2,format=rgba,pad={item}:{item2}:(ow-iw)/2:(oh-ih)/2:black,format=yuv420p[v]",
			"-map",
			"[v]",
			"-map",
			"1:a:0"
		} : new List<string>(8)
		{
			"-i",
			file,
			"-filter_complex",
			$"[0:a]showwaves=s={(item - item / 8) / 2 * 2}x{item2 / 3 / 2 * 2}:mode=cline:rate=30:colors={value}:scale=sqrt:draw=full,format=rgba,pad={item}:{item2}:(ow-iw)/2:(oh-ih)/2:black,format=yuv420p[v]",
			"-map",
			"[v]",
			"-map",
			"0:a:0"
		});
		string[] h = FFmpeg.H264;
		int num = 0;
		string[] array = new string[8 + h.Length];
		ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(h);
		readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		array[num] = "-c:a";
		num++;
		array[num] = FFmpeg.Aac;
		num++;
		array[num] = "-b:a";
		num++;
		array[num] = "192k";
		num++;
		array[num] = "-shortest";
		num++;
		array[num] = "-movflags";
		num++;
		array[num] = "+faststart";
		num++;
		array[num] = output;
		list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(array));
		try
		{
			await FFmpeg.Run(list, mediaInfo.Duration, context.ProgressFor(0, 1), context.Cancellation);
			return output;
		}
		catch
		{
			MediaOps.TryDelete(output);
			throw;
		}
	}

	public static string ChannelFilter(MediaInfo info, bool mono, double left, double right, int? solo = null)
	{
		if (info.AudioChannels < 2)
		{
			if (solo.HasValue)
			{
				return "pan=mono|c0=c0";
			}
			if (mono)
			{
				return "pan=mono|c0=" + N((left + right) / 2.0) + "*c0";
			}
			return $"pan=stereo|c0={N(left)}*c0|c1={N(right)}*c0";
		}
		if (solo.HasValue)
		{
			int valueOrDefault = solo.GetValueOrDefault();
			return $"pan=mono|c0=c{valueOrDefault != 0}";
		}
		if (!mono)
		{
			return $"pan=stereo|c0={N(left)}*c0|c1={N(right)}*c1";
		}
		return $"pan=mono|c0={N(left * 0.5)}*c0+{N(right * 0.5)}*c1";
	}

	public static async Task<string> ConvertChannels(string file, bool mono, double left, double right, OperationContext context)
	{
		MediaInfo mediaInfo = await MediaProbe.Info(file);
		if (!mediaInfo.HasAudio)
		{
			throw new ShortTechException("Choose Mono or Stereo for a file with an audio track.");
		}
		List<string> list = new List<string>
		{
			"-map",
			"0:v?",
			"-map",
			"0:a:0",
			"-map_metadata",
			"0",
			"-af",
			ChannelFilter(mediaInfo, mono, left, right)
		};
		if (mediaInfo.HasVideo)
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c:v", "copy" }));
		}
		list.AddRange(MediaOps.AudioCodec(FileKinds.Ext(file), mediaInfo.AudioBits >= 24));
		string output = Output.PathFor(file, FileKinds.Ext(file), mono ? "mono" : "stereo");
		await MediaOps.Encode(file, output, list, mediaInfo, context.ProgressFor(0, 1), null, context);
		return output;
	}

	public static string BleepFilter(IReadOnlyList<(double Start, double End)> ranges, double duration)
	{
		string value = string.Join("+", ranges.Select(((double Start, double End) r) => $"between(t\\,{N(r.Start)}\\,{N(r.End)})"));
		return $"[0:a:0]aresample=48000,volume=0:enable='{value}'[muted];sine=frequency=1000:sample_rate=48000:duration={N(duration)},volume=0.3,volume=0:enable='not({value})'[tone];" + "[muted][tone]amix=inputs=2:duration=first:normalize=0[a]";
	}

	public static async Task<string> Bleep(string file, IReadOnlyList<(double Start, double End)> ranges, OperationContext? context, string? target = null)
	{
		if (ranges.Count == 0)
		{
			throw new ShortTechException("Add at least one bleep range.");
		}
		MediaInfo mediaInfo = await MediaProbe.Info(file);
		if (!mediaInfo.HasAudio)
		{
			throw new ShortTechException("This file has no audio track.");
		}
		string text = ((target != null) ? FileKinds.Ext(target) : FileKinds.Ext(file));
		string output = target ?? Output.PathFor(file, text, "bleeped");
		List<string> list = new List<string>
		{
			"-filter_complex",
			BleepFilter(ranges, mediaInfo.Duration),
			"-map",
			"0:v?",
			"-map",
			"[a]",
			"-map_metadata",
			"0"
		};
		if (mediaInfo.HasVideo && target == null)
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c:v", "copy" }));
		}
		list.AddRange(MediaOps.AudioCodec(text, mediaInfo.AudioBits >= 24));
		await MediaOps.Encode(file, output, list, mediaInfo, context?.ProgressFor(0, 1), null, context);
		return output;
	}

	public static async Task<string> PreviewAudio(string file, string filter, bool complex)
	{
		string output = Path.Combine(Path.GetTempPath(), "ShortTech", $"preview-{Guid.NewGuid():N}.m4a");
		Directory.CreateDirectory(Path.GetDirectoryName(output));
		string[] array = ((!complex) ? new string[5] { "-vn", "-map", "0:a:0", "-af", filter } : new string[4] { "-filter_complex", filter, "-map", "[a]" });
		string[] array2 = array;
		int num = 0;
		string[] array3 = new string[4 + array2.Length];
		ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(array2);
		readOnlySpan.CopyTo(new Span<string>(array3).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		array3[num] = "-c:a";
		num++;
		array3[num] = FFmpeg.Aac;
		num++;
		array3[num] = "-b:a";
		num++;
		array3[num] = "160k";
		await MediaOps.Encode(file, output, new global::_003C_003Ez__ReadOnlyArray<string>(array3), new MediaInfo(), null);
		return output;
	}

	public static async Task<byte[]?> Thumbnail(string file, double time, int height = 120)
	{
		try
		{
			return await FFmpeg.Capture(new global::_003C_003Ez__ReadOnlyArray<string>(new string[13]
			{
				"-ss",
				FFmpeg.Seconds(time),
				"-i",
				file,
				"-frames:v",
				"1",
				"-vf",
				$"scale=-2:{height}",
				"-f",
				"image2pipe",
				"-vcodec",
				"png",
				"-"
			}));
		}
		catch (ShortTechException)
		{
			return null;
		}
	}
}
