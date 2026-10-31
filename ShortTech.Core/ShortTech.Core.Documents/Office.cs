using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ShortTech.Core.Documents;

public static class Office
{
	public static bool HasWord => Type.GetTypeFromProgID("Word.Application") != null;

	public static bool HasPowerPoint => Type.GetTypeFromProgID("PowerPoint.Application") != null;

	public static string? LibreOffice
	{
		get
		{
			string[] array = new string[2]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
			};
			for (int i = 0; i < array.Length; i++)
			{
				string text = Path.Combine(array[i], "LibreOffice", "program", "soffice.exe");
				if (File.Exists(text))
				{
					return text;
				}
			}
			return ProcessRunner.Find("soffice");
		}
	}

	public static bool CanRenderSlides
	{
		get
		{
			if (!HasPowerPoint)
			{
				return LibreOffice != null;
			}
			return true;
		}
	}

	public static async Task<Doc> WordToDoc(string path)
	{
		using TempFolder temp = Output.Temp();
		string docx = temp.File("document.docx");
		if (HasWord)
		{
			await Sta(delegate
			{
				dynamic val = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application"));
				try
				{
					val.Visible = false;
					val.DisplayAlerts = 0;
					dynamic val2 = val.Documents.Open(path, false, true);
					val2.SaveAs2(docx, 16);
					val2.Close(false);
				}
				finally
				{
					val.Quit();
					Marshal.FinalReleaseComObject(val);
				}
			});
		}
		else
		{
			string libreOffice = LibreOffice;
			if (libreOffice == null)
			{
				throw new ShortTechException("Microsoft Word is needed for this file.");
			}
			await Convert(libreOffice, path, "docx", temp.Path);
			docx = Path.Combine(temp.Path, Path.GetFileNameWithoutExtension(path) + ".docx");
		}
		Doc doc = Docx.Read(docx);
		doc.Title = Path.GetFileNameWithoutExtension(path);
		return doc;
	}

	public static async Task SlidesToPdf(string path, string output)
	{
		if (HasPowerPoint)
		{
			await Sta(delegate
			{
				dynamic val = Activator.CreateInstance(Type.GetTypeFromProgID("PowerPoint.Application"));
				try
				{
					dynamic val2 = val.Presentations.Open(path, -1, 0, 0);
					val2.SaveAs(output, 32);
					val2.Close();
				}
				finally
				{
					val.Quit();
					Marshal.FinalReleaseComObject(val);
				}
			});
			return;
		}
		string libreOffice = LibreOffice;
		if (libreOffice != null)
		{
			using (TempFolder temp = Output.Temp())
			{
				await Convert(libreOffice, path, "pdf", temp.Path);
				File.Move(Path.Combine(temp.Path, Path.GetFileNameWithoutExtension(path) + ".pdf"), output, overwrite: true);
				return;
			}
		}
		throw new ShortTechException("Microsoft PowerPoint is needed for this file.");
	}

	public static async Task SlidesToPptx(string path, string output)
	{
		if (HasPowerPoint)
		{
			await Sta(delegate
			{
				dynamic val = Activator.CreateInstance(Type.GetTypeFromProgID("PowerPoint.Application"));
				try
				{
					dynamic val2 = val.Presentations.Open(path, -1, 0, 0);
					val2.SaveAs(output, 24);
					val2.Close();
				}
				finally
				{
					val.Quit();
					Marshal.FinalReleaseComObject(val);
				}
			});
			return;
		}
		string libreOffice = LibreOffice;
		if (libreOffice != null)
		{
			using (TempFolder temp = Output.Temp())
			{
				await Convert(libreOffice, path, "pptx", temp.Path);
				File.Move(Path.Combine(temp.Path, Path.GetFileNameWithoutExtension(path) + ".pptx"), output, overwrite: true);
				return;
			}
		}
		throw new ShortTechException("Microsoft PowerPoint is needed for this file.");
	}

	private static async Task Convert(string soffice, string path, string format, string folder)
	{
		if ((await ProcessRunner.Run(soffice, new global::_003C_003Ez__ReadOnlyArray<string>(new string[7] { "--headless", "--norestore", "--convert-to", format, "--outdir", folder, path }))).ExitCode != 0)
		{
			throw new ShortTechException("LibreOffice could not convert " + Path.GetFileName(path) + ".");
		}
	}

	private static Task Sta(Action action)
	{
		return Task.Run(delegate
		{
			ShortTech.Core.Documents.Sta.Run(action);
		});
	}
}
