using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace ShortTech.Core;

public static class ArchiveOps
{
	public static string Title(string format)
	{
		return format.ToUpperInvariant();
	}

	public static string? FormatOf(string path)
	{
		return FileKinds.Ext(path) switch
		{
			"zip" => "zip", 
			"tar" => "tar", 
			"tgz" => "tgz", 
			"gz" => "gz", 
			_ => null, 
		};
	}

	public static async Task<IReadOnlyList<string>> Pack(IReadOnlyList<string> files, string format, OperationContext context)
	{
		context.Report($"Packing {files.Count} items");
		string text = ((files.Count == 1) ? Path.GetFileName(files[0].TrimEnd('\\', '/')) : "Archive");
		string output = ((format == "gz") ? Output.Unique(Path.Combine(Output.Directory(files[0]), Path.GetFileName(files[0]) + ".gz")) : Output.Named(text + "." + ((format == "tgz") ? "tar.gz" : format), files[0]));
		await Task.Run(delegate
		{
			switch (format)
			{
			case "zip":
			{
				using ZipArchive zip = ZipFile.Open(output, ZipArchiveMode.Create);
				foreach (string file in files)
				{
					AddToZip(zip, file, Path.GetFileName(file.TrimEnd('\\', '/')));
				}
				break;
			}
			case "tar":
			{
				using FileStream stream4 = File.Create(output);
				WriteTar(stream4, files);
				break;
			}
			case "tgz":
			{
				using FileStream stream2 = File.Create(output);
				using GZipStream stream3 = new GZipStream(stream2, CompressionLevel.Optimal);
				WriteTar(stream3, files);
				break;
			}
			case "gz":
			{
				using FileStream fileStream = File.OpenRead(files[0]);
				using FileStream stream = File.Create(output);
				using GZipStream destination = new GZipStream(stream, CompressionLevel.Optimal);
				fileStream.CopyTo(destination);
				break;
			}
			}
		});
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
	}

	private static void AddToZip(ZipArchive zip, string path, string entryName)
	{
		if (Directory.Exists(path))
		{
			List<string> list = Directory.EnumerateFileSystemEntries(path).ToList();
			if (list.Count == 0)
			{
				zip.CreateEntry(entryName + "/");
			}
			{
				foreach (string item in list)
				{
					AddToZip(zip, item, entryName + "/" + Path.GetFileName(item));
				}
				return;
			}
		}
		zip.CreateEntryFromFile(path, entryName, CompressionLevel.Optimal);
	}

	private static void WriteTar(Stream stream, IReadOnlyList<string> files)
	{
		TarWriter tar = new TarWriter(stream, TarEntryFormat.Pax, leaveOpen: true);
		try
		{
			foreach (string file in files)
			{
				Add(file, Path.GetFileName(file.TrimEnd('\\', '/')));
			}
		}
		finally
		{
			if (tar != null)
			{
				((IDisposable)tar).Dispose();
			}
		}
		void Add(string path, string name)
		{
			if (Directory.Exists(path))
			{
				tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, name + "/"));
				{
					foreach (string item in Directory.EnumerateFileSystemEntries(path))
					{
						Add(item, name + "/" + Path.GetFileName(item));
					}
					return;
				}
			}
			tar.WriteEntry(path, name);
		}
	}

	public static async Task<IReadOnlyList<string>> Extract(IReadOnlyList<string> files, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			context.Report("Extracting " + Path.GetFileName(file));
			string text = Path.GetFileName(file);
			string[] array = new string[13]
			{
				".tar.gz", ".tgz", ".tar.bz2", ".tar.xz", ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2",
				".xz", ".iso", ".cab"
			};
			foreach (string text2 in array)
			{
				if (text.EndsWith(text2, StringComparison.OrdinalIgnoreCase))
				{
					string text3 = text;
					int length = text2.Length;
					text = text3.Substring(0, text3.Length - length);
					break;
				}
			}
			string folder = Output.Named(text, file);
			await Task.Run(delegate
			{
				ExtractTo(file, folder, context);
			});
			List<string> list = Directory.EnumerateFileSystemEntries(folder).ToList();
			if (list.Count == 1 && Directory.Exists(list[0]))
			{
				string text4 = Output.Unique(Path.Combine(Path.GetDirectoryName(folder), Path.GetFileName(list[0])));
				string text5 = folder + ".shorttech-tmp";
				Directory.Move(folder, text5);
				Directory.Move(Path.Combine(text5, Path.GetFileName(list[0])), text4);
				Directory.Delete(text5, recursive: true);
				folder = text4;
			}
			outputs.Add(folder);
		}
		return outputs;
	}

	private static void ExtractTo(string file, string folder, OperationContext context)
	{
		Directory.CreateDirectory(folder);
		string text = FileKinds.Ext(file);
		if (text == "gz" && !Path.GetFileNameWithoutExtension(file).EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
		{
			using (FileStream stream = File.OpenRead(file))
			{
				using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
				using FileStream destination = File.Create(Path.Combine(folder, Path.GetFileNameWithoutExtension(file)));
				gZipStream.CopyTo(destination);
				return;
			}
		}
		try
		{
			using IArchive archive = ArchiveFactory.Open(file);
			if (archive.IsSolid || archive.Type == ArchiveType.SevenZip || text == "tgz")
			{
				using IReader reader = archive.ExtractAllEntries();
				reader.WriteAllToDirectory(folder, new ExtractionOptions
				{
					ExtractFullPath = true,
					Overwrite = true
				});
			}
			else
			{
				List<IArchiveEntry> list = archive.Entries.Where((IArchiveEntry e) => !e.IsDirectory).ToList();
				for (int num = 0; num < list.Count; num++)
				{
					context.Progress((double)num / (double)Math.Max(1, list.Count));
					list[num].WriteToDirectory(folder, new ExtractionOptions
					{
						ExtractFullPath = true,
						Overwrite = true
					});
				}
			}
			if (!Directory.EnumerateFileSystemEntries(folder).Any())
			{
				throw new ShortTechException("The source archive is empty.");
			}
		}
		catch (Exception ex) when (((ex is CryptographicException || ex is InvalidFormatException || ex is SharpCompressException) ? 1 : 0) != 0)
		{
			Directory.Delete(folder, recursive: true);
			throw new ShortTechException("This archive is encrypted or uses a format Short Tech cannot open.");
		}
	}

	public static async Task<IReadOnlyList<string>> Convert(IReadOnlyList<string> files, string format, OperationContext context)
	{
		List<string> outputs = new List<string>();
		foreach (string file in files)
		{
			using TempFolder temp = Output.Temp();
			string root = Path.Combine(temp.Path, "x");
			await Task.Run(delegate
			{
				ExtractTo(file, root, context);
			});
			List<string> items = Directory.EnumerateFileSystemEntries(root).ToList();
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
			if (fileNameWithoutExtension.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
			{
				string text = fileNameWithoutExtension;
				text.Substring(0, text.Length - 4);
			}
			string output = Output.PathFor(file, (format == "tgz") ? "tar.gz" : format);
			await Task.Run(delegate
			{
				string text2 = format;
				if (text2 == "zip")
				{
					using ZipArchive zip = ZipFile.Open(output, ZipArchiveMode.Create);
					foreach (string item in items)
					{
						AddToZip(zip, item, Path.GetFileName(item));
					}
					return;
				}
				if (text2 == "tar")
				{
					using FileStream stream = File.Create(output);
					WriteTar(stream, items);
					return;
				}
				using FileStream stream2 = File.Create(output);
				using GZipStream stream3 = new GZipStream(stream2, CompressionLevel.Optimal);
				WriteTar(stream3, items);
			});
			outputs.Add(output);
		}
		return outputs;
	}
}
