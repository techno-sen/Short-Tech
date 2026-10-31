using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShortTech.Core.Images;
using ShortTech.Core.Media;
using ImageMagick;

namespace ShortTech.Core;

public static class Compression
{
	public static async Task<IReadOnlyList<string>> Images(IReadOnlyList<string> files, CompressSettings settings, OperationContext context)
	{
		List<string> outputs = new List<string>();
		long before = 0L;
		long after = 0L;
		int skipped = 0;
		for (int index = 0; index < files.Count; index++)
		{
			context.Report($"Compressing {index + 1} of {files.Count}");
			context.Progress((double)index / (double)files.Count);
			string file = files[index];
			long original = ImageEngine.Size(file);
			MagickImage image = await ImageEngine.LoadAsync(file);
			try
			{
				ImageFormat format = ImageOps.CompressedFormat(file, image);
				string output = Output.PathFor(file, format.Ext, "compressed");
				CompressTarget target = settings.Target;
				if ((object)target != null)
				{
					byte[] bytes = await Task.Run(() => Fit(image, format, target.For(original), settings.ImageMaxPixels));
					await File.WriteAllBytesAsync(output, bytes);
					goto IL_03df;
				}
				await Task.Run(delegate
				{
					int? imageMaxPixels = settings.ImageMaxPixels;
					if (imageMaxPixels.HasValue)
					{
						int valueOrDefault = imageMaxPixels.GetValueOrDefault();
						if (Math.Max(image.Width, image.Height) > valueOrDefault)
						{
							image.Resize(new MagickGeometry((uint)valueOrDefault, (uint)valueOrDefault));
						}
					}
					ImageEngine.Save(image, format, output, settings.Preset.ImageQuality());
				});
				if (settings.ImageMaxPixels.HasValue || ImageEngine.Size(output) < original)
				{
					goto IL_03df;
				}
				File.Delete(output);
				skipped++;
				goto end_IL_01ab;
				IL_03df:
				before += original;
				after += ImageEngine.Size(output);
				outputs.Add(output);
				end_IL_01ab:;
			}
			finally
			{
				if (image != null)
				{
					((IDisposable)image).Dispose();
				}
			}
		}
		if (outputs.Count == 0 && skipped > 0)
		{
			throw new ShortTechException("Already as small as this preset gets. Try Strong or a target size.");
		}
		context.Finish(Savings("image", outputs.Count, before, after));
		return outputs;
	}

	private static byte[] Fit(MagickImage source, ImageFormat format, long limit, int? maxPixels)
	{
		if (limit < 1024)
		{
			throw new ShortTechException("Enter a target size of at least 1 KB.");
		}
		int num = (int)Math.Max(source.Width, source.Height);
		int num2 = (maxPixels.HasValue ? Math.Min(maxPixels.Value, num) : num);
		long num3 = long.MaxValue;
		for (int i = 0; i < 8; i++)
		{
			if (num2 < 32)
			{
				break;
			}
			using MagickImage magickImage = (MagickImage)source.Clone();
			if (num2 < num)
			{
				magickImage.Resize(new MagickGeometry((uint)num2, (uint)num2));
			}
			byte[] array = BestQuality(magickImage, format, limit);
			if (array != null)
			{
				return array;
			}
			num3 = Math.Min(num3, ImageEngine.Encode(magickImage, format, 0.05).LongLength);
			num2 = (int)((double)num2 * 0.8);
		}
		throw new ShortTechException("Could not reach the target size. Smallest attempt was " + Format.Bytes(num3) + ".");
	}

	private static byte[]? BestQuality(MagickImage image, ImageFormat format, long limit)
	{
		byte[] array = ImageEngine.Encode(image, format, 0.95);
		if (array.LongLength <= limit)
		{
			return array;
		}
		double num = 0.05;
		double num2 = 0.95;
		byte[] array2 = null;
		for (int i = 0; i < 7; i++)
		{
			double num3 = (num + num2) / 2.0;
			byte[] array3 = ImageEngine.Encode(image, format, num3);
			if (array3.LongLength <= limit)
			{
				array2 = array3;
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		if (array2 == null)
		{
			byte[] array4 = ImageEngine.Encode(image, format, 0.05);
			if (array4.LongLength <= limit)
			{
				array2 = array4;
			}
		}
		return array2;
	}

	internal static string Savings(string noun, int count, long before, long after)
	{
		string text = $"{count} {noun}{((count == 1) ? "" : "s")}";
		if (before <= 0)
		{
			return "Compressed " + text;
		}
		int num = 100 - (int)Math.Round((double)after / (double)before * 100.0);
		if (num <= 0)
		{
			return "Compressed " + text + " · already as small as it gets";
		}
		return $"Compressed {text} · {num}% smaller";
	}

	public static async Task<IReadOnlyList<string>> Videos(IReadOnlyList<string> files, CompressSettings settings, OperationContext context)
	{
		List<string> outputs = new List<string>();
		long before = 0L;
		long after = 0L;
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			context.Report($"Compressing {index + 1} of {files.Count}");
			MediaInfo info = await MediaProbe.Info(file);
			if (!info.HasVideo || info.Duration <= 0.0)
			{
				throw new ShortTechException(Path.GetFileName(file) + " has no video track");
			}
			double audioBits = (info.HasAudio ? 128000 : 0);
			double videoBits = VideoBitrate(info, settings, audioBits);
			string output = Output.PathFor(file, "mp4", "compressed");
			long? limit = settings.Target?.For(info.FileSize);
			for (int attempt = 0; attempt < 3; attempt++)
			{
				string item = ((long)videoBits).ToString();
				List<string> list = new List<string>
				{
					"-map",
					"0:v:0",
					"-map",
					"0:a?",
					"-map_metadata",
					"0",
					"-c:v",
					"libx265",
					"-preset",
					"medium",
					"-b:v",
					item,
					"-maxrate",
					((long)(videoBits * 1.1)).ToString(),
					"-bufsize",
					item,
					"-tag:v",
					"hvc1",
					"-x265-params",
					"log-level=error"
				};
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
				{
					"-pix_fmt",
					info.IsHighDepth ? "yuv420p10le" : "yuv420p"
				}));
				int? videoMaxHeight = settings.VideoMaxHeight;
				if (videoMaxHeight.HasValue)
				{
					int valueOrDefault = videoMaxHeight.GetValueOrDefault();
					if (info.Height > valueOrDefault)
					{
						list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
						{
							"-vf",
							$"scale=-2:{valueOrDefault}"
						}));
					}
				}
				if (info.HasAudio)
				{
					list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[4]
					{
						"-c:a",
						FFmpeg.Aac,
						"-b:a",
						"128k"
					}));
				}
				list.AddRange(new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { "-movflags", "+faststart" }));
				if (attempt > 0)
				{
					context.Report($"Fitting {index + 1} of {files.Count} to the target size");
				}
				await MediaOps.Encode(file, output, list, info, context.ProgressFor(index, files.Count), null, context);
				long num = ImageEngine.Size(output);
				if (!limit.HasValue)
				{
					break;
				}
				long valueOrDefault2 = limit.GetValueOrDefault();
				if (num <= valueOrDefault2 || videoBits <= 100000.0)
				{
					break;
				}
				videoBits = Math.Max(100000.0, videoBits / ((double)num / (double)valueOrDefault2) * 0.95);
				if (attempt == 2)
				{
					MediaOps.TryDelete(output);
					throw new ShortTechException($"Could not fit {Path.GetFileName(file)} within {Format.Bytes(valueOrDefault2)}. Choose a larger size.");
				}
			}
			before += info.FileSize;
			after += ImageEngine.Size(output);
			outputs.Add(output);
		}
		context.Finish(Savings("video", files.Count, before, after));
		return outputs;
	}

	private static double VideoBitrate(MediaInfo info, CompressSettings settings, double audioBits)
	{
		CompressTarget target = settings.Target;
		if ((object)target != null)
		{
			return Math.Max(150000.0, (double)(target.For(info.FileSize) * 8) / info.Duration * 0.97 - audioBits);
		}
		double num = info.Bitrate * settings.Preset.MediaShare() - audioBits;
		int? videoMaxHeight = settings.VideoMaxHeight;
		if (videoMaxHeight.HasValue)
		{
			int valueOrDefault = videoMaxHeight.GetValueOrDefault();
			if (info.Height > valueOrDefault)
			{
				num *= (double)valueOrDefault * (double)valueOrDefault / ((double)info.Height * (double)info.Height);
			}
		}
		return Math.Max(300000.0, num);
	}

	public static async Task<IReadOnlyList<string>> Audio(IReadOnlyList<string> files, CompressSettings settings, OperationContext context)
	{
		List<string> outputs = new List<string>();
		long before = 0L;
		long after = 0L;
		for (int index = 0; index < files.Count; index++)
		{
			string file = files[index];
			context.Report($"Compressing {index + 1} of {files.Count}");
			MediaInfo info = await MediaProbe.Info(file);
			if (!info.HasAudio || info.Duration <= 0.0)
			{
				throw new ShortTechException(Path.GetFileName(file) + " has no audio track");
			}
			long num = (long)AudioBitrate(info, settings);
			var (item, item2) = FileKinds.Ext(file) switch
			{
				"mp3" => ("libmp3lame", "mp3"),
				"opus" => ("libopus", "opus"),
				"ogg" => ("libvorbis", "ogg"),
				_ => (FFmpeg.Aac, "m4a"),
			};
			string output = Output.PathFor(file, item2, "compressed");
			await MediaOps.Encode(file, output, new global::_003C_003Ez__ReadOnlyArray<string>(new string[9]
			{
				"-vn",
				"-map",
				"0:a:0",
				"-map_metadata",
				"0",
				"-c:a",
				item,
				"-b:a",
				num.ToString()
			}), info, context.ProgressFor(index, files.Count), null, context);
			before += info.FileSize;
			after += ImageEngine.Size(output);
			outputs.Add(output);
		}
		context.Finish(Savings("file", files.Count, before, after));
		return outputs;
	}

	private static double AudioBitrate(MediaInfo info, CompressSettings settings)
	{
		CompressTarget target = settings.Target;
		if ((object)target != null)
		{
			return Math.Min(320000.0, Math.Max(32000.0, (double)(target.For(info.FileSize) * 8) / info.Duration * 0.97));
		}
		return Math.Min(320000.0, Math.Max(64000.0, info.Bitrate * settings.Preset.MediaShare()));
	}

	public static async Task<long?> Estimate(IReadOnlyList<string> files, FileKind kind, CompressSettings settings)
	{
		switch (kind)
		{
		case FileKind.Image:
		{
			CompressTarget target2 = settings.Target;
			if ((object)target2 != null)
			{
				return files.Sum((string path) => Math.Min(ImageEngine.Size(path), target2.For(ImageEngine.Size(path))));
			}
			List<string> sample = files.Take(4).ToList();
			long num3 = 0L;
			foreach (string file in sample)
			{
				try
				{
					long num = num3;
					num3 = num + await Task.Run(delegate
					{
						using MagickImage image = ImageEngine.Load(file, settings.ImageMaxPixels);
						return Math.Min(ImageEngine.Encode(image, ImageOps.CompressedFormat(file, image), settings.Preset.ImageQuality()).LongLength, ImageEngine.Size(file));
					});
				}
				catch (MagickException)
				{
					return null;
				}
			}
			return num3 * files.Count / Math.Max(sample.Count, 1);
		}
		case FileKind.Video:
		case FileKind.Audio:
		{
			long num = 0L;
			foreach (string file2 in files)
			{
				MediaInfo mediaInfo = await MediaProbe.Info(file2);
				if (mediaInfo.Duration <= 0.0)
				{
					return null;
				}
				CompressTarget target = settings.Target;
				if ((object)target != null)
				{
					num += Math.Min(mediaInfo.FileSize, target.For(mediaInfo.FileSize));
				}
				else if (kind == FileKind.Video)
				{
					double num2 = (mediaInfo.HasAudio ? 128000 : 0);
					num += Math.Min(mediaInfo.FileSize, (long)((VideoBitrate(mediaInfo, settings, num2) + num2) * mediaInfo.Duration / 8.0));
				}
				else
				{
					num += Math.Min(mediaInfo.FileSize, (long)(AudioBitrate(mediaInfo, settings) * mediaInfo.Duration / 8.0));
				}
			}
			return num;
		}
		default:
			return null;
		}
	}
}
