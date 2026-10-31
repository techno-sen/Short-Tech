using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShortTech.Core.Media;

public static class MediaOps
{
	public sealed record Loudness(string I, string Tp, string Lra, string Thresh, string Offset);

	private static readonly Dictionary<string, HashSet<string>> CopyableVideo = new Dictionary<string, HashSet<string>>
	{
		["mp4"] = new HashSet<string> { "h264", "hevc", "av1" },
		["mov"] = new HashSet<string> { "h264", "hevc", "prores", "mjpeg" },
		["mkv"] = new HashSet<string> { "h264", "hevc", "vp9", "av1", "prores", "mpeg4" },
		["webm"] = new HashSet<string> { "vp9", "av1" }
	};

	private static readonly Dictionary<string, HashSet<string>> CopyableAudio = new Dictionary<string, HashSet<string>>
	{
		["mp4"] = new HashSet<string> { "aac", "alac", "mp3" },
		["mov"] = new HashSet<string> { "aac", "alac", "mp3", "pcm" },
		["mkv"] = new HashSet<string> { "aac", "alac", "mp3", "pcm", "opus", "flac", "ac3", "eac3", "vorbis" },
		["webm"] = new HashSet<string> { "opus", "vorbis" }
	};

	private const string SilenceEdge = "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1";

	private const string SilenceFilter = "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1,areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1,areverse";

	public static string[] ConversionArguments(MediaFormat format, MediaInfo info)
	{
		if (format.AudioOnly)
		{
			return AudioArguments(format, info);
		}
		if (!info.HasVideo)
		{
			throw new ShortTechException("This file has no video track");
		}
		if (format == MediaFormat.Gif)
		{
			return new string[5] { "-an", "-vf", "fps=12,scale='min(640,iw)':-2:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4", "-loop", "0" };
		}
		string ext = format.Ext;
		HashSet<string> value;
		bool num = CopyableVideo.TryGetValue(ext, out value) && value.Contains(info.VideoCodec ?? "");
		HashSet<string> value2;
		bool flag = !info.HasAudio || (CopyableAudio.TryGetValue(ext, out value2) && value2.Contains(info.AudioCodec ?? ""));
		List<string> list = new List<string> { "-map", "0:v:0", "-map", "0:a?", "-map_metadata", "0" };
		if (num)
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c:v", "copy" }));
			if (info.VideoCodec == "hevc" && ext != "mkv")
			{
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-tag:v", "hvc1" }));
			}
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-strict", "unofficial" }));
		}
		else if (ext == "webm")
		{
			list.AddRange(FFmpeg.WebMVideo());
		}
		else
		{
			list.AddRange(info.IsHighDepth ? FFmpeg.Hevc10 : FFmpeg.H264);
		}
		if (flag)
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c:a", "copy" }));
		}
		else if (ext == "webm")
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[4] { "-c:a", "libopus", "-b:a", "128k" }));
		}
		else
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[4]
			{
				"-c:a",
				FFmpeg.Aac,
				"-b:a",
				"192k"
			}));
		}
		if ((ext == "mp4" || ext == "mov") ? true : false)
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-movflags", "+faststart" }));
		}
		return list.ToArray();
	}

	private static string[] AudioArguments(MediaFormat format, MediaInfo info)
	{
		if (!info.HasAudio)
		{
			throw new ShortTechException("This file has no audio track");
		}
		string text = info.AudioCodec ?? "";
		bool flag = info.AudioBits >= 24;
		string[] array = new string[5] { "-vn", "-map", "0:a:0", "-map_metadata", "0" };
		string ext = format.Ext;
		if (ext == null)
		{
			goto IL_035b;
		}
		int length = ext.Length;
		string[] array2;
		if (length != 3)
		{
			if (length != 4)
			{
				goto IL_035b;
			}
			char c = ext[0];
			if (c != 'a')
			{
				if (c != 'f')
				{
					if (c != 'o' || !(ext == "opus"))
					{
						goto IL_035b;
					}
					array2 = ((!(text == "opus")) ? new string[4] { "-c:a", "libopus", "-b:a", "160k" } : new string[2] { "-c:a", "copy" });
				}
				else
				{
					if (!(ext == "flac"))
					{
						goto IL_035b;
					}
					array2 = ((!(text == "flac")) ? new string[2] { "-c:a", "flac" } : new string[2] { "-c:a", "copy" });
				}
			}
			else
			{
				if (!(ext == "aiff"))
				{
					goto IL_035b;
				}
				array2 = new string[2]
				{
					"-c:a",
					flag ? "pcm_s24be" : "pcm_s16be"
				};
			}
		}
		else
		{
			char c = ext[1];
			if ((uint)c <= 97u)
			{
				if (c != '4')
				{
					if (c != 'a' || !(ext == "wav"))
					{
						goto IL_035b;
					}
					array2 = new string[2]
					{
						"-c:a",
						flag ? "pcm_s24le" : "pcm_s16le"
					};
				}
				else
				{
					if (!(ext == "m4a"))
					{
						goto IL_035b;
					}
					bool flag2 = ((text == "alac" || text == "aac") ? true : false);
					array2 = ((!flag2) ? new string[4]
					{
						"-c:a",
						FFmpeg.Aac,
						"-b:a",
						"256k"
					} : new string[2] { "-c:a", "copy" });
				}
			}
			else if (c != 'g')
			{
				if (c != 'p' || !(ext == "mp3"))
				{
					goto IL_035b;
				}
				array2 = ((!(text == "mp3")) ? new string[4] { "-c:a", "libmp3lame", "-q:a", "2" } : new string[2] { "-c:a", "copy" });
			}
			else
			{
				if (!(ext == "ogg"))
				{
					goto IL_035b;
				}
				array2 = ((!(text == "vorbis")) ? new string[4] { "-c:a", "libvorbis", "-q:a", "5" } : new string[2] { "-c:a", "copy" });
			}
		}
		goto IL_0361;
		IL_035b:
		array2 = Array.Empty<string>();
		goto IL_0361;
		IL_0361:
		string[] array3 = array2;
		array2 = array;
		string[] array4 = array3;
		length = 0;
		string[] array5 = new string[array2.Length + array4.Length];
		ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(array2);
		readOnlySpan.CopyTo(new Span<string>(array5).Slice(length, readOnlySpan.Length));
		length += readOnlySpan.Length;
		ReadOnlySpan<string> readOnlySpan2 = new ReadOnlySpan<string>(array4);
		readOnlySpan2.CopyTo(new Span<string>(array5).Slice(length, readOnlySpan2.Length));
		length += readOnlySpan2.Length;
		return array5;
	}

	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, MediaFormat format, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Converting {index + 1} of {files.Count} to {format.Name}");
			string file = files[index];
			MediaInfo info = await MediaProbe.Info(file);
			string output = Output.PathFor(file, format.Ext);
			await Encode(file, output, ConversionArguments(format, info), info, context.ProgressFor(index, files.Count), null, context);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task Encode(string file, string output, IEnumerable<string> arguments, MediaInfo info, Action<double>? progress, IEnumerable<string>? inputOptions = null, OperationContext? context = null)
	{
		try
		{
			await FFmpeg.Run(file, output, arguments, info.Duration, progress, inputOptions, context?.Cancellation ?? default(CancellationToken));
		}
		catch
		{
			TryDelete(output);
			throw;
		}
	}

	public static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	public static Task<IReadOnlyList<string>> Mute(IReadOnlyList<string> files, OperationContext context)
	{
		return StreamCopy(files, "muted", context, new string[5] { "-map", "0:v", "-c", "copy", "-an" });
	}

	public static async Task<IReadOnlyList<string>> StripMetadata(IReadOnlyList<string> files, OperationContext context)
	{
		IReadOnlyList<string> result = await StreamCopy(files, "clean", context, new string[16]
		{
			"-map", "0:v?", "-map", "0:a?", "-map_metadata", "-1", "-map_chapters", "-1", "-fflags", "+bitexact",
			"-flags:v", "+bitexact", "-flags:a", "+bitexact", "-c", "copy"
		});
		context.Finish((files.Count == 1) ? "Metadata removed from 1 file" : $"Metadata removed from {files.Count} files");
		return result;
	}

	private static async Task<IReadOnlyList<string>> StreamCopy(IReadOnlyList<string> files, string suffix, OperationContext context, string[] arguments)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			MediaInfo info = await MediaProbe.Info(file);
			string output = Output.PathFor(file, FileKinds.Ext(file), suffix);
			await Encode(file, output, arguments, info, context.ProgressFor(index, files.Count), null, context);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Rotate(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			MediaInfo info = await MediaProbe.Info(file);
			string output = Output.PathFor(file, FileKinds.Ext(file), "rotated");
			await Encode(file, output, new global::_003C_003Ez__ReadOnlyArray<string>(new string[8] { "-map", "0:v:0", "-map", "0:a?", "-map_metadata", "0", "-c", "copy" }), info, null, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-display_rotation:v:0", "-90" }), context);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> Join(IReadOnlyList<string> files, OperationContext context)
	{
		if (files.Count < 2)
		{
			throw new ShortTechException("Select at least two videos to join.");
		}
		List<MediaInfo> infos = new List<MediaInfo>();
		foreach (string file in files)
		{
			List<MediaInfo> list = infos;
			list.Add(await MediaProbe.Info(file));
		}
		if (!infos.All((MediaInfo info) => info.HasVideo))
		{
			throw new ShortTechException("Every selected file must contain a video track.");
		}
		MediaInfo first = infos[0];
		double total = infos.Sum((MediaInfo info) => info.Duration);
		string output = Output.Named(Path.GetFileNameWithoutExtension(files[0]) + " joined.mp4", files[0]);
		Action<double> progress = context.ProgressFor(0, 1);
		bool flag = infos.All((MediaInfo info) => info.VideoCodec == first.VideoCodec && info.Width == first.Width && info.Height == first.Height && info.AudioCodec == first.AudioCodec && info.IsHighDepth == first.IsHighDepth);
		if (flag)
		{
			string videoCodec = first.VideoCodec;
			bool flag2 = ((videoCodec == "h264" || videoCodec == "hevc") ? true : false);
			flag = flag2;
		}
		bool flag3 = flag;
		if (flag3)
		{
			string videoCodec = first.AudioCodec;
			bool flag2 = ((videoCodec == null || videoCodec == "aac") ? true : false);
			flag3 = flag2;
		}
		if (flag3)
		{
			context.Report($"Joining {files.Count} videos");
			using TempFolder temp = Output.Temp();
			string list2 = temp.File("list.txt");
			await File.WriteAllLinesAsync(list2, files.Select((string file) => "file '" + file.Replace("'", "'\\''") + "'"));
			List<string> list3 = new List<string>
			{
				"-f", "concat", "-safe", "0", "-i", list2, "-map", "0:v", "-map", "0:a?",
				"-c", "copy"
			};
			if (first.VideoCodec == "hevc")
			{
				list3.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-tag:v", "hvc1" }));
			}
			list3.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[3] { "-movflags", "+faststart", output }));
			try
			{
				await FFmpeg.Run(list3, total, progress, context.Cancellation);
				return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
			}
			catch (ShortTechException)
			{
				TryDelete(output);
			}
		}
		context.Report($"Encoding {files.Count} videos into one");
		int value = ((first.Width > 0) ? first.Width : 1920);
		int value2 = ((first.Height > 0) ? first.Height : 1080);
		bool flag4 = infos.Any((MediaInfo info) => info.IsHighDepth);
		List<string> list4 = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		for (int num = 0; num < files.Count; num++)
		{
			list4.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
			{
				"-i",
				files[num]
			}));
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(102, 7, stringBuilder2);
			handler.AppendLiteral("[");
			handler.AppendFormatted(num);
			handler.AppendLiteral(":v]scale=");
			handler.AppendFormatted(value);
			handler.AppendLiteral(":");
			handler.AppendFormatted(value2);
			handler.AppendLiteral(":force_original_aspect_ratio=decrease,pad=");
			handler.AppendFormatted(value);
			handler.AppendLiteral(":");
			handler.AppendFormatted(value2);
			handler.AppendLiteral(":(ow-iw)/2:(oh-ih)/2,setsar=1,fps=30,format=");
			handler.AppendFormatted(flag4 ? "yuv420p10le" : "yuv420p");
			handler.AppendLiteral("[v");
			handler.AppendFormatted(num);
			handler.AppendLiteral("];");
			stringBuilder3.Append(ref handler);
		}
		int num2 = files.Count;
		for (int num3 = 0; num3 < infos.Count; num3++)
		{
			if (infos[num3].HasAudio)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(78, 2, stringBuilder2);
				handler.AppendLiteral("[");
				handler.AppendFormatted(num3);
				handler.AppendLiteral(":a]aformat=sample_rates=48000:channel_layouts=stereo,asetpts=PTS-STARTPTS[a");
				handler.AppendFormatted(num3);
				handler.AppendLiteral("];");
				stringBuilder4.Append(ref handler);
			}
			else
			{
				list4.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[6]
				{
					"-f",
					"lavfi",
					"-t",
					FFmpeg.Seconds(infos[num3].Duration),
					"-i",
					"anullsrc=r=48000:cl=stereo"
				}));
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(28, 2, stringBuilder2);
				handler.AppendLiteral("[");
				handler.AppendFormatted(num2);
				handler.AppendLiteral(":a]asetpts=PTS-STARTPTS[a");
				handler.AppendFormatted(num3);
				handler.AppendLiteral("];");
				stringBuilder5.Append(ref handler);
				num2++;
			}
		}
		stringBuilder.Append(string.Concat(string.Concat(from i in Enumerable.Range(0, files.Count)
			select $"[v{i}][a{i}]"), $"concat=n={files.Count}:v=1:a=1[v][a]"));
		try
		{
			List<string> list5 = new List<string>();
			list5.AddRange(list4);
			list5.Add("-filter_complex");
			list5.Add(stringBuilder.ToString());
			list5.Add("-map");
			list5.Add("[v]");
			list5.Add("-map");
			list5.Add("[a]");
			list5.AddRange(flag4 ? FFmpeg.Hevc10 : FFmpeg.H264);
			list5.Add("-c:a");
			list5.Add(FFmpeg.Aac);
			list5.Add("-b:a");
			list5.Add("192k");
			list5.Add("-movflags");
			list5.Add("+faststart");
			list5.Add(output);
			await FFmpeg.Run(new _003C_003Ez__ReadOnlyList<string>(list5), total, progress, context.Cancellation);
		}
		catch
		{
			TryDelete(output);
			throw;
		}
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
	}

	public static async Task<IReadOnlyList<string>> JoinAudio(IReadOnlyList<string> files, OperationContext context)
	{
		if (files.Count < 2)
		{
			throw new ShortTechException("Select at least two audio files to join.");
		}
		List<MediaInfo> infos = new List<MediaInfo>();
		foreach (string file in files)
		{
			infos.Add(await MediaProbe.Info(file));
		}
		if (!infos.All((MediaInfo info) => info.HasAudio))
		{
			throw new ShortTechException("Every selected file must contain an audio track.");
		}
		string ext = FileKinds.Ext(files[0]);
		if (!MediaFormat.Audio.Any((MediaFormat format) => format.Ext == ext))
		{
			ext = "m4a";
		}
		string output = Output.Named(Path.GetFileNameWithoutExtension(files[0]) + " joined." + ext, files[0]);
		context.Report($"Joining {files.Count} audio files");
		List<string> arguments = new List<string>();
		StringBuilder filter = new StringBuilder();
		for (int i = 0; i < files.Count; i++)
		{
			arguments.Add("-i");
			arguments.Add(files[i]);
			filter.Append($"[{i}:a:0]aformat=sample_rates=48000:channel_layouts=stereo,asetpts=PTS-STARTPTS[a{i}];");
		}
		filter.Append(string.Concat(Enumerable.Range(0, files.Count).Select((int i) => $"[a{i}]")));
		filter.Append($"concat=n={files.Count}:v=0:a=1[a]");
		arguments.Add("-filter_complex");
		arguments.Add(filter.ToString());
		arguments.Add("-map");
		arguments.Add("[a]");
		arguments.AddRange(AudioCodec(ext, infos.Any((MediaInfo info) => info.AudioBits >= 24)));
		arguments.Add(output);
		try
		{
			await FFmpeg.Run(arguments, infos.Sum((MediaInfo info) => info.Duration), context.ProgressFor(0, 1), context.Cancellation);
		}
		catch
		{
			TryDelete(output);
			throw;
		}
		return new string[1] { output };
	}

	public static async Task<string> Trim(string file, double start, double end, bool trimSilence, OperationContext context)
	{
		string ext = FileKinds.Ext(file);
		string output = Output.PathFor(file, ext, "trimmed");
		MediaInfo mediaInfo = await MediaProbe.Info(file);
		double num = Math.Max(0.05, end - start);
		string[] inputOptions = new string[4]
		{
			"-ss",
			FFmpeg.Seconds(start),
			"-t",
			FFmpeg.Seconds(num)
		};
		if (trimSilence)
		{
			List<string> list = new List<string>();
			list.Add("-vn");
			list.Add("-map");
			list.Add("0:a:0");
			list.Add("-map_metadata");
			list.Add("0");
			list.Add("-af");
			list.Add("silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1,areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1,areverse");
			list.AddRange(AudioCodec(ext, mediaInfo.AudioBits >= 24));
			await Encode(file, output, new _003C_003Ez__ReadOnlyList<string>(list), mediaInfo with
			{
				Duration = num
			}, context.ProgressFor(0, 1), inputOptions, context);
			return output;
		}
		await CutSegment(file, start, end, output, mediaInfo, context.ProgressFor(0, 1), context);
		return output;
	}

	public static async Task CutSegment(string file, double start, double end, string output, MediaInfo info, Action<double>? progress, OperationContext? context = null)
	{
		string text = FileKinds.Ext(file);
		double num = Math.Max(0.05, end - start);
		List<string> list = new List<string> { "-map", "0:v?", "-map", "0:a?", "-map_metadata", "0" };
		if (info.HasVideo)
		{
			if (text == "webm")
			{
				string[] array = FFmpeg.WebMVideo(30);
				int num2 = 0;
				string[] array2 = new string[4 + array.Length];
				ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(array);
				readOnlySpan.CopyTo(new Span<string>(array2).Slice(num2, readOnlySpan.Length));
				num2 += readOnlySpan.Length;
				array2[num2] = "-c:a";
				num2++;
				array2[num2] = "libopus";
				num2++;
				array2[num2] = "-b:a";
				num2++;
				array2[num2] = "160k";
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(array2));
			}
			else
			{
				string[] array2 = (info.IsHighDepth ? FFmpeg.Hevc10 : FFmpeg.H264);
				int num2 = 0;
				string[] array = new string[4 + array2.Length];
				ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(array2);
				readOnlySpan.CopyTo(new Span<string>(array).Slice(num2, readOnlySpan.Length));
				num2 += readOnlySpan.Length;
				array[num2] = "-c:a";
				num2++;
				array[num2] = FFmpeg.Aac;
				num2++;
				array[num2] = "-b:a";
				num2++;
				array[num2] = "192k";
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(array));
			}
			bool flag;
			switch (text)
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
			if (flag)
			{
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-movflags", "+faststart" }));
			}
		}
		else
		{
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c", "copy" }));
		}
		await Encode(file, output, list, info with
		{
			Duration = num
		}, progress, new global::_003C_003Ez__ReadOnlyArray<string>(new string[4]
		{
			"-ss",
			FFmpeg.Seconds(start),
			"-t",
			FFmpeg.Seconds(num)
		}), context);
	}

	public static async Task<float[]> Waveform(string file, int bars, int? channel = null)
	{
		object obj;
		if (!channel.HasValue)
		{
			obj = new string[2] { "-ac", "1" };
		}
		else
		{
			int valueOrDefault = channel.GetValueOrDefault();
			obj = new string[2]
			{
				"-af",
				$"pan=mono|c0=c{valueOrDefault}"
			};
		}
		string[] collection = (string[])obj;
		byte[] array;
		try
		{
			List<string> list = new List<string>();
			list.Add("-i");
			list.Add(file);
			list.Add("-vn");
			list.AddRange(collection);
			list.Add("-ar");
			list.Add("4000");
			list.Add("-f");
			list.Add("f32le");
			list.Add("-");
			array = await FFmpeg.Capture(new _003C_003Ez__ReadOnlyList<string>(list));
		}
		catch (ShortTechException)
		{
			return Array.Empty<float>();
		}
		float[] array2 = new float[array.Length / 4];
		Buffer.BlockCopy(array, 0, array2, 0, array2.Length * 4);
		if (array2.Length == 0 || bars <= 0)
		{
			return Array.Empty<float>();
		}
		int num = Math.Max(1, array2.Length / bars);
		List<float> list2 = new List<float>();
		for (int i = 0; i < array2.Length; i += num)
		{
			if (list2.Count >= bars)
			{
				break;
			}
			float num2 = 0f;
			for (int j = i; j < Math.Min(i + num, array2.Length); j++)
			{
				num2 = Math.Max(num2, Math.Abs(array2[j]));
			}
			list2.Add(num2);
		}
		float top = list2.Max();
		return (top > 0f) ? list2.Select((float p) => p / top).ToArray() : list2.ToArray();
	}

	public static async Task<IReadOnlyList<string>> Normalize(IReadOnlyList<string> files, OperationContext context)
	{
		return await Normalize(files, -16.0, 11.0, -1.5, context);
	}

	public static async Task<IReadOnlyList<string>> Normalize(IReadOnlyList<string> files, double integrated, double range, double peak, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			context.Report($"Measuring loudness {index + 1} of {files.Count}");
			MediaInfo info = await MediaProbe.Info(file);
			if (!info.HasAudio)
			{
				throw new ShortTechException(Path.GetFileName(file) + " has no audio track");
			}
			string text = $"I={Num(integrated)}:TP={Num(peak)}:LRA={Num(range)}";
			string filter = "loudnorm=" + text;
			Loudness loudness = await MeasureLoudness(file, text);
			if ((object)loudness != null)
			{
				filter += $":measured_I={loudness.I}:measured_TP={loudness.Tp}:measured_LRA={loudness.Lra}:measured_thresh={loudness.Thresh}:offset={loudness.Offset}:linear=true";
			}
			context.Report($"Normalizing {index + 1} of {files.Count}");
			string text2 = FileKinds.Ext(file);
			List<string> list = new List<string> { "-map", "0:v?", "-map", "0:a", "-map_metadata", "0", "-af", filter };
			if (info.HasVideo)
			{
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c:v", "copy" }));
			}
			list.AddRange(AudioCodec(text2, info.AudioBits >= 24));
			list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-ar", "48000" }));
			string output = Output.PathFor(file, text2, "normalized");
			await Encode(file, output, list, info, context.ProgressFor(index, files.Count), null, context);
			outputs.Add(output);
		}
		return outputs;
	}

	private static string Num(double value)
	{
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	public static async Task<Loudness?> MeasureLoudness(string file, string target = "I=-16:TP=-1.5:LRA=11")
	{
		string text = await FFmpeg.Report(new global::_003C_003Ez__ReadOnlyArray<string>(new string[8]
		{
			"-i",
			file,
			"-vn",
			"-af",
			"loudnorm=" + target + ":print_format=json",
			"-f",
			"null",
			"-"
		}));
		int num = text.LastIndexOf('{');
		int num2 = text.LastIndexOf('}');
		if (num < 0 || num2 < num)
		{
			return null;
		}
		try
		{
			int num3 = num;
			using JsonDocument jsonDocument = JsonDocument.Parse(text.Substring(num3, num2 + 1 - num3));
			JsonElement root = jsonDocument.RootElement;
			return new Loudness(Get("input_i"), Get("input_tp"), Get("input_lra"), Get("input_thresh"), Get("target_offset"));
			string Get(string key)
			{
				return root.GetProperty(key).GetString() ?? "0";
			}
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public static async Task<IReadOnlyList<string>> ToMono(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			MediaInfo mediaInfo = await MediaProbe.Info(file);
			if (!mediaInfo.HasAudio)
			{
				throw new ShortTechException(Path.GetFileName(file) + " has no audio track");
			}
			List<string> list = new List<string> { "-map", "0:v?", "-map", "0:a", "-map_metadata", "0", "-ac", "1" };
			if (mediaInfo.HasVideo)
			{
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-c:v", "copy" }));
			}
			list.AddRange(AudioCodec(FileKinds.Ext(file), mediaInfo.AudioBits >= 24));
			string output = Output.PathFor(file, FileKinds.Ext(file), "mono");
			await Encode(file, output, list, mediaInfo, context.ProgressFor(index, files.Count), null, context);
			outputs.Add(output);
		}
		return outputs;
	}

	public static async Task<IReadOnlyList<string>> TrimSilence(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			MediaInfo mediaInfo = await MediaProbe.Info(file);
			string output = Output.PathFor(file, FileKinds.Ext(file), "trimmed");
			List<string> list = new List<string>();
			list.Add("-vn");
			list.Add("-map");
			list.Add("0:a:0");
			list.Add("-map_metadata");
			list.Add("0");
			list.Add("-af");
			list.Add("silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1,areverse,silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.1,areverse");
			list.AddRange(AudioCodec(FileKinds.Ext(file), mediaInfo.AudioBits >= 24));
			await Encode(file, output, new _003C_003Ez__ReadOnlyList<string>(list), mediaInfo, context.ProgressFor(index, files.Count), null, context);
			outputs.Add(output);
		}
		return outputs;
	}

	public static string[] AudioCodec(string ext, bool deep)
	{
		if (ext != null)
		{
			int length = ext.Length;
			if (length != 3)
			{
				if (length == 4)
				{
					char c = ext[0];
					if ((uint)c <= 102u)
					{
						if (c != 'a')
						{
							if (c == 'f' && ext == "flac")
							{
								return new string[2] { "-c:a", "flac" };
							}
						}
						else if (ext == "aiff")
						{
							goto IL_0249;
						}
					}
					else if (c != 'o')
					{
						if (c == 'w' && ext == "webm")
						{
							goto IL_02b4;
						}
					}
					else if (ext == "opus")
					{
						goto IL_02b4;
					}
				}
			}
			else
			{
				switch (ext[1])
				{
				case 'p':
					if (!(ext == "mp3"))
					{
						if (ext == "mp4")
						{
							goto IL_01f7;
						}
						goto IL_0306;
					}
					return new string[4] { "-c:a", "libmp3lame", "-q:a", "2" };
				case '4':
					if (ext == "m4a" || ext == "m4v")
					{
						goto IL_01f7;
					}
					goto IL_0306;
				case 'o':
					if (ext == "mov")
					{
						goto IL_01f7;
					}
					goto IL_0306;
				case 'a':
					if (ext == "aac")
					{
						goto IL_01f7;
					}
					if (!(ext == "wav"))
					{
						goto IL_0306;
					}
					return new string[2]
					{
						"-c:a",
						deep ? "pcm_s24le" : "pcm_s16le"
					};
				case 'i':
					break;
				case 'g':
					if (!(ext == "ogg"))
					{
						goto IL_0306;
					}
					return new string[4] { "-c:a", "libvorbis", "-q:a", "5" };
				case 'k':
					goto IL_014d;
				case 'm':
					if (!(ext == "wma"))
					{
						goto IL_0306;
					}
					return new string[4] { "-c:a", "wmav2", "-b:a", "192k" };
				default:
					goto IL_0306;
					IL_01f7:
					return new string[4]
					{
						"-c:a",
						FFmpeg.Aac,
						"-b:a",
						"192k"
					};
				}
				if (ext == "aif")
				{
					goto IL_0249;
				}
			}
		}
		goto IL_0306;
		IL_0249:
		return new string[2]
		{
			"-c:a",
			deep ? "pcm_s24be" : "pcm_s16be"
		};
		IL_014d:
		if (ext == "mkv")
		{
			goto IL_02b4;
		}
		goto IL_0306;
		IL_02b4:
		return new string[4] { "-c:a", "libopus", "-b:a", "160k" };
		IL_0306:
		return Array.Empty<string>();
	}

	public static async Task Frame(string file, double time, string output)
	{
		MediaInfo info = await MediaProbe.Info(file);
		await Encode(file, output, new global::_003C_003Ez__ReadOnlyArray<string>(new string[4] { "-frames:v", "1", "-update", "1" }), info, null, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
		{
			"-ss",
			FFmpeg.Seconds(time)
		}));
	}
}
