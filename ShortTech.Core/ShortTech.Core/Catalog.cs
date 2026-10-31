using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ShortTech.Core.Documents;
using ShortTech.Core.Images;
using ShortTech.Core.Media;
using ShortTech.Core.Pdf;

namespace ShortTech.Core;

public static class Catalog
{
	public static class Icons
	{
		public const string Photo = "\ue91b";

		public const string Document = "\ue8a5";

		public const string Film = "\ue8b2";

		public const string Audio = "\ue8d6";

		public const string Archive = "\uf012";

		public const string Compress = "\ue73f";

		public const string Metadata = "\ue946";

		public const string Edit = "\ue70f";

		public const string Annotate = "\uee56";

		public const string Background = "\ue771";

		public const string Crop = "\ue7a8";

		public const string Redact = "\ued1a";

		public const string Rotate = "\ue7ad";

		public const string Text = "\ue8c1";

		public const string Qr = "\ued14";

		public const string Pdf = "\uea90";

		public const string Collage = "\uf0e2";

		public const string Merge = "\uf5ed";

		public const string Organize = "\uf0e2";

		public const string Split = "\ue8c6";

		public const string Trim = "\ue78c";

		public const string Speed = "\uec4a";

		public const string Camera = "\ue722";

		public const string Join = "\ue71b";

		public const string Mute = "\ue74f";

		public const string Volume = "\ue767";

		public const string Waveform = "\ue9d9";

		public const string Channels = "\ue7f3";

		public const string Bleep = "\ue7ba";

		public const string Silence = "\ue992";

		public const string Share = "\ue72d";

		public const string Float = "\ue840";

		public const string Copy = "\ue8c8";

		public const string Move = "\ue8de";

		public const string Path = "\ue71b";

		public const string Terminal = "\ue756";

		public const string Extract = "\uf012";

		public const string Folder = "\ue8b7";
	}

	public static IReadOnlyList<ShortTechOperation> For(WheelMode mode, IReadOnlyList<string> files)
	{
		if (files.Count == 0)
		{
			return Array.Empty<ShortTechOperation>();
		}
		return mode switch
		{
			WheelMode.Convert => Conversions(files), 
			WheelMode.Tools => Tools(files), 
			_ => Actions(), 
		};
	}

	public static ShortTechOperation? Find(string id, IReadOnlyList<string> files)
	{
		return Enum.GetValues<WheelMode>().SelectMany((WheelMode mode) => For(mode, files)).FirstOrDefault((ShortTechOperation op) => op.Id == id);
	}

	private static List<ShortTechOperation> Conversions(IReadOnlyList<string> files)
	{
		switch (files.CommonKind())
		{
		case FileKind.Image:
		{
			HashSet<ImageFormat> sources3 = files.Select(ImageFormat.Of).ToHashSet();
			List<ShortTechOperation> list2 = (from format in ImageFormat.All
				where (sources3.Count != 1 || !sources3.Contains(format)) && format.CanWrite
				select new ShortTechOperation("image." + format.Ext, "To " + format.Name, "\ue91b", (IReadOnlyList<string> f, OperationContext c) => ImageOps.Convert(f, format, c))
				{
					Badge = format.Name
				}).ToList();
			list2.Add(new ShortTechOperation("image.pdf", "To PDF", "\uea90", ImageOps.MakePdf)
			{
				Badge = "PDF"
			});
			return list2;
		}
		case FileKind.Pdf:
		{
			int num = 8;
			List<ShortTechOperation> list3 = new List<ShortTechOperation>(num);
			CollectionsMarshal.SetCount(list3, num);
			Span<ShortTechOperation> span = CollectionsMarshal.AsSpan(list3);
			span[0] = new ShortTechOperation("pdf.docx", "To Word", "\ue8a5", (IReadOnlyList<string> f, OperationContext c) => PdfOps.ToDocument(f, DocumentTarget.Docx, c))
			{
				Badge = "DOCX"
			};
			span[1] = new ShortTechOperation("pdf.pptx", "To PowerPoint", "\ue8a5", SlideOps.PdfToPptx)
			{
				Badge = "PPTX"
			};
			span[2] = new ShortTechOperation("pdf.epub", "To EPUB", "\ue8a5", (IReadOnlyList<string> f, OperationContext c) => PdfOps.ToDocument(f, DocumentTarget.Epub, c))
			{
				Badge = "EPUB"
			};
			span[3] = new ShortTechOperation("pdf.md", "To Markdown", "\ue8a5", (IReadOnlyList<string> f, OperationContext c) => PdfOps.ToDocument(f, DocumentTarget.Md, c))
			{
				Badge = "MD"
			};
			span[4] = new ShortTechOperation("pdf.png", "Pages to PNG", "\ue91b", (IReadOnlyList<string> f, OperationContext c) => PdfOps.RenderPages(f, ImageFormat.Png, c))
			{
				Badge = "PNG"
			};
			span[5] = new ShortTechOperation("pdf.jpg", "Pages to JPEG", "\ue91b", (IReadOnlyList<string> f, OperationContext c) => PdfOps.RenderPages(f, ImageFormat.Jpeg, c))
			{
				Badge = "JPG"
			};
			span[6] = new ShortTechOperation("pdf.txt", "To Text", "\ue8c1", PdfOps.ToText)
			{
				Badge = "TXT"
			};
			span[7] = new ShortTechOperation("pdf.xlsx", "To Excel", "\ue8a5", PdfOps.ToExcel)
			{
				Badge = "XLSX"
			};
			return list3;
		}
		case FileKind.Video:
			return MediaFormat.Video.Where((MediaFormat format) => format.Ext != FileKinds.Ext(files[0]) || files.Count > 1).Select(MediaConversion).ToList();
		case FileKind.Audio:
			return MediaFormat.Audio.Where((MediaFormat format) => format.Ext != FileKinds.Ext(files[0]) || files.Count > 1).Select(MediaConversion).ToList();
		case FileKind.Document:
		{
			HashSet<string> sources4 = files.Select((string f) => (!(FileKinds.Ext(f) == "markdown")) ? ((!(FileKinds.Ext(f) == "htm")) ? FileKinds.Ext(f) : "html") : "md").ToHashSet();
			return (from target in DocumentTargets.All
				where sources4.Count != 1 || !sources4.Contains(target.Ext())
				select new ShortTechOperation("doc." + target.Ext(), "To " + target.Title(), "\ue8a5", (IReadOnlyList<string> f, OperationContext c) => DocumentOps.Convert(f, target, c))
				{
					Badge = target.Title()
				}).ToList();
		}
		case FileKind.Spreadsheet:
		{
			HashSet<string> sources = files.Select(FileKinds.Ext).ToHashSet();
			return (from target in SheetOps.Targets
				where sources.Count != 1 || !sources.Contains(target)
				select new ShortTechOperation("sheet." + target, "To " + SheetOps.Title(target), "\ue8a5", (IReadOnlyList<string> f, OperationContext c) => SheetOps.Convert(f, target, c))
				{
					Badge = SheetOps.Title(target)
				}).ToList();
		}
		case FileKind.Presentation:
		{
			if (!Office.CanRenderSlides)
			{
				return new List<ShortTechOperation>();
			}
			HashSet<string> sources5 = files.Select(FileKinds.Ext).ToHashSet();
			return (from target in SlideOps.Targets
				where sources5.Count != 1 || !sources5.Contains(target)
				select new ShortTechOperation("slides." + target, "To " + SlideOps.Title(target), "\ue8a5", (IReadOnlyList<string> f, OperationContext c) => SlideOps.Convert(f, target, c))
				{
					Badge = SlideOps.Title(target)
				}).ToList();
		}
		case FileKind.Subtitle:
		{
			HashSet<string> sources2 = files.Select(FileKinds.Ext).ToHashSet();
			return (from target in SubtitleOps.Targets
				where sources2.Count != 1 || !sources2.Contains(target)
				select new ShortTechOperation("subtitle." + target, "To " + SubtitleOps.Title(target), "\ue8c1", (IReadOnlyList<string> f, OperationContext c) => SubtitleOps.Convert(f, target, c))
				{
					Badge = SubtitleOps.Title(target)
				}).ToList();
		}
		case FileKind.Archive:
		{
			string current = ((files.Count == 1) ? ArchiveOps.FormatOf(files[0]) : null);
			return (from format in new string[3] { "zip", "tar", "tgz" }
				where format != current
				select new ShortTechOperation("archive.to-" + format, "To " + ArchiveOps.Title(format), "\uf012", (IReadOnlyList<string> f, OperationContext c) => ArchiveOps.Convert(f, format, c))
				{
					Badge = ArchiveOps.Title(format)
				}).ToList();
		}
		default:
		{
			List<string> list = new List<string> { "zip", "tar", "tgz" };
			if (files.Count == 1 && files.CommonKind() != FileKind.Folder)
			{
				list.Add("gz");
			}
			return list.Select((string format) => new ShortTechOperation("archive." + format, "To " + ArchiveOps.Title(format), "\uf012", (IReadOnlyList<string> f, OperationContext c) => ArchiveOps.Pack(f, format, c))
			{
				Badge = ArchiveOps.Title(format)
			}).ToList();
		}
		}
	}

	private static ShortTechOperation MediaConversion(MediaFormat format)
	{
		return new ShortTechOperation("media." + format.Ext, "To " + format.Name, format.AudioOnly ? "\ue8d6" : "\ue8b2", (IReadOnlyList<string> f, OperationContext c) => MediaOps.Convert(f, format, c))
		{
			Badge = format.Name
		};
	}

	private static ShortTechOperation Tool(string id, string title, string? @short, string icon, OperationWork run, string? window = null, bool producesFiles = true)
	{
		return new ShortTechOperation(id, title, icon, run)
		{
			Short = @short,
			Window = window,
			ProducesFiles = producesFiles
		};
	}

	private static Task<IReadOnlyList<string>> NeedsWindow(IReadOnlyList<string> files, OperationContext context)
	{
		throw new ShortTechException("This tool needs its window.");
	}

	private static List<ShortTechOperation> Tools(IReadOnlyList<string> files)
	{
		HashSet<FileKind> hashSet = files.Kinds();
		bool flag = files.Count == 1;
		List<ShortTechOperation> list = new List<ShortTechOperation>();
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlySingleElementList<FileKind>(FileKind.Image)))
		{
			list.Add(Tool("image.compress", "Compress", null, "\ue73f", (IReadOnlyList<string> f, OperationContext c) => Compression.Images(f, CompressSettings.Saved, c), "compress"));
			list.Add(Tool("image.metadata", "Edit Metadata", "Metadata", "\ue946", ImageOps.RemoveMetadata, flag ? "metadata" : null));
			if (flag)
			{
				list.Add(Tool("image.edit", "Edit Photo", "Edit", "\ue70f", NeedsWindow, "edit"));
				list.Add(Tool("image.annotate", "Annotate", null, "\uee56", NeedsWindow, "annotate"));
			}
			list.Add(Tool("image.resize", "Resize Images", "Resize", "\ue740", (IReadOnlyList<string> f, OperationContext c) => ImageOps.Resize(f, Settings.Current.ResizeMaxPixels, c), "resize"));
			list.Add(Tool("image.frame", "Add Background", "Add BG", "\ue771", (IReadOnlyList<string> f, OperationContext c) => Frames.Apply(f, new FrameOptions(), c), "frame"));
			list.Add(Tool("image.crop", "Crop", null, "\ue7a8", ImageOps.SquareCrop, "crop"));
			if (flag)
			{
				list.Add(Tool("image.redact", "Redact", null, "\ued1a", NeedsWindow, "redact"));
			}
			list.Add(Tool("image.rotate", "Rotate", null, "\ue7ad", ImageOps.Rotate));
			list.Add(Tool("image.ocr", "Copy Text", "Text", "\ue8c1", ImageOps.ExtractText));
			list.Add(Tool("image.qr", "Read QR Code", "QR Code", "\ued14", ImageOps.ReadQrCodes, null, producesFiles: false));
			if (!flag)
			{
				list.Add(Tool("image.topdf", "Create PDF", "PDF", "\uea90", ImageOps.MakePdf));
				list.Add(Tool("image.collage", "Create Collage", "Collage", "\uf0e2", (IReadOnlyList<string> f, OperationContext c) => Collage.Make(f, new CollageOptions(), c), "collage"));
			}
		}
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlySingleElementList<FileKind>(FileKind.Pdf)))
		{
			list.Add(Tool("pdf.compress", "Compress", null, "\ue73f", PdfOps.Compress));
			list.Add(Tool("pdf.metadata", "Remove Metadata", "Metadata", "\ue946", PdfOps.StripMetadata));
			list.Add(flag ? Tool("pdf.organize", "Organize Pages", "Organize", "\uf0e2", NeedsWindow, "organize") : Tool("pdf.merge", "Merge PDFs", "Merge", "\uf5ed", PdfOps.Merge, "merge"));
			list.Add(Tool("pdf.split", "Split Pages", "Split", "\ue8c6", PdfOps.Split));
			list.Add(Tool("pdf.protect", "Protect with Password", "Lock", "\ue72e", NeedsWindow, "protect"));
			list.Add(Tool("pdf.ocr", "Copy Text", "Text", "\ue8c1", ImageOps.ExtractText));
		}
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlySingleElementList<FileKind>(FileKind.Video)))
		{
			list.Add(Tool("video.compress", "Compress", null, "\ue73f", (IReadOnlyList<string> f, OperationContext c) => Compression.Videos(f, CompressSettings.Saved, c), "compress"));
			list.Add(Tool("video.metadata", "Remove Metadata", "Metadata", "\ue946", MediaOps.StripMetadata));
			if (flag)
			{
				list.Add(Tool("video.trim", "Trim", null, "\ue78c", NeedsWindow, "trim"));
				list.Add(Tool("video.crop", "Crop Video", "Crop", "\ue7a8", NeedsWindow, "crop-video"));
				list.Add(Tool("video.speed", "Change Speed", "Speed", "\uec4a", NeedsWindow, "speed"));
				list.Add(Tool("video.frames", "Save Frames", "Frames", "\ue722", NeedsWindow, "frames"));
				list.Add(Tool("video.split", "Split Video", "Split", "\ue8c6", NeedsWindow, "split"));
				list.Add(Tool("video.redact", "Redact Video", "Redact", "\ued1a", NeedsWindow, "redact-video"));
			}
			else
			{
				list.Add(Tool("video.join", "Join Videos", "Join", "\ue71b", MediaOps.Join, "join"));
			}
			list.Add(Tool("video.rotate", "Rotate", null, "\ue7ad", MediaOps.Rotate));
			list.Add(Tool("video.mute", "Mute", null, "\ue74f", MediaOps.Mute));
			list.Add(Tool("video.normalize", "Normalize Volume", "Normalize", "\ue767", MediaOps.Normalize, flag ? "normalize" : null));
		}
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlySingleElementList<FileKind>(FileKind.Audio)))
		{
			list.Add(Tool("audio.compress", "Compress", null, "\ue73f", (IReadOnlyList<string> f, OperationContext c) => Compression.Audio(f, CompressSettings.Saved, c), "compress"));
			list.Add(Tool("audio.metadata", "Remove Metadata", "Metadata", "\ue946", MediaOps.StripMetadata));
			list.Add(Tool("audio.normalize", "Normalize Volume", "Normalize", "\ue767", MediaOps.Normalize, flag ? "normalize" : null));
			if (flag)
			{
				list.Add(Tool("audio.visualizer", "Audio Visualizer", "Visualizer", "\ue9d9", NeedsWindow, "visualizer"));
				list.Add(Tool("audio.trim", "Trim", null, "\ue78c", NeedsWindow, "trim"));
				list.Add(Tool("audio.speed", "Change Speed", "Speed", "\uec4a", NeedsWindow, "speed"));
				list.Add(Tool("audio.channels", "Convert Channels", "Channels", "\ue7f3", MediaOps.ToMono, "channels"));
				list.Add(Tool("audio.bleep", "Bleep Audio", "Bleep", "\ue7ba", NeedsWindow, "bleep"));
			}
			else
			{
				list.Add(Tool("audio.join", "Join Audio", "Join", "\ue71b", MediaOps.JoinAudio, "join-audio"));
				list.Add(Tool("audio.silence", "Trim Silence", "Silence", "\ue992", MediaOps.TrimSilence));
				list.Add(Tool("audio.mono", "To Mono", "Mono", "\ue7f3", MediaOps.ToMono));
			}
		}
		if (QrContent.CanEncode(files))
		{
			list.Add(Tool("text.qr", "Create QR Code", "Make QR", "\ued14", QrContent.Quick, "qr"));
		}
		if (hashSet.SetEquals(new global::_003C_003Ez__ReadOnlySingleElementList<FileKind>(FileKind.Archive)))
		{
			list.Add(Tool("archive.extract", "Extract", null, "\uf012", ArchiveOps.Extract));
		}
		return list;
	}

	private static List<ShortTechOperation> Actions()
	{
		return new List<ShortTechOperation>
		{
			Tool("action.share", "Share", null, "\ue72d", FileActions.Share, null, producesFiles: false),
			Tool("action.float", "Float", null, "\ue840", FileActions.NewFloat, null, producesFiles: false),
			Tool("action.copy", "Copy", null, "\ue8c8", FileActions.CopyFiles, null, producesFiles: false),
			Tool("action.move", "Move", null, "\ue8de", FileActions.Move, null, producesFiles: false),
			Tool("action.rename", "Rename", null, "\ue8ac", NeedsWindow, "rename", producesFiles: false),
			Tool("action.paths", "Copy Path", null, "\ue71b", FileActions.CopyPaths, null, producesFiles: false),
			Tool("action.terminal", "Terminal", null, "\ue756", FileActions.OpenTerminal, null, producesFiles: false)
		};
	}
}
