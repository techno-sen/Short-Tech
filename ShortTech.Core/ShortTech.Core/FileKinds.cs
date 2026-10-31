using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShortTech.Core;

public static class FileKinds
{
	public static readonly HashSet<string> Images = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"jpg", "jpeg", "jfif", "png", "heic", "heif", "webp", "avif", "tif", "tiff",
		"gif", "bmp", "ico", "svg", "dng", "cr2", "cr3", "nef", "arw", "raf",
		"orf", "rw2", "psd", "tga", "jxl"
	};

	public static readonly HashSet<string> Videos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"mp4", "m4v", "mov", "mkv", "webm", "avi", "wmv", "flv", "mpg", "mpeg",
		"ts", "mts", "m2ts", "3gp", "ogv"
	};

	public static readonly HashSet<string> Audios = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"mp3", "m4a", "aac", "wav", "flac", "aif", "aiff", "ogg", "oga", "opus",
		"wma", "alac", "caf", "amr"
	};

	public static readonly HashSet<string> Documents = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "txt", "rtf", "doc", "docx", "odt", "html", "htm", "md", "markdown", "epub" };

	public static readonly HashSet<string> Spreadsheets = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "xlsx", "xls", "csv", "tsv", "ods" };

	public static readonly HashSet<string> Presentations = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pptx", "ppt", "odp" };

	public static readonly HashSet<string> Subtitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "srt", "vtt" };

	public static readonly HashSet<string> Archives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"zip", "tar", "gz", "tgz", "bz2", "xz", "rar", "7z", "tbz", "txz",
		"iso", "cab"
	};

	public static FileKind Of(string path)
	{
		if (Directory.Exists(path))
		{
			return FileKind.Folder;
		}
		string text = Ext(path);
		if (Spreadsheets.Contains(text))
		{
			return FileKind.Spreadsheet;
		}
		if (Presentations.Contains(text))
		{
			return FileKind.Presentation;
		}
		if (Subtitles.Contains(text))
		{
			return FileKind.Subtitle;
		}
		if (text == "pdf")
		{
			return FileKind.Pdf;
		}
		if (Images.Contains(text))
		{
			return FileKind.Image;
		}
		if (Videos.Contains(text))
		{
			return FileKind.Video;
		}
		if (Audios.Contains(text))
		{
			return FileKind.Audio;
		}
		if (Documents.Contains(text))
		{
			return FileKind.Document;
		}
		if (Archives.Contains(text))
		{
			return FileKind.Archive;
		}
		return FileKind.Other;
	}

	public static string Ext(string path)
	{
		string text = Path.GetFileName(path).ToLowerInvariant();
		if (text.EndsWith(".tar.gz"))
		{
			return "tgz";
		}
		return Path.GetExtension(text).TrimStart('.');
	}

	public static HashSet<FileKind> Kinds(this IEnumerable<string> files)
	{
		return files.Select(Of).ToHashSet();
	}

	public static FileKind? CommonKind(this IReadOnlyCollection<string> files)
	{
		HashSet<FileKind> hashSet = files.Kinds();
		if (hashSet.Count != 1)
		{
			return null;
		}
		return hashSet.First();
	}
}
