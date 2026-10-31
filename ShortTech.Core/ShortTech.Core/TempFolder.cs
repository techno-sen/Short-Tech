using System;
using System.IO;

namespace ShortTech.Core;

public sealed class TempFolder : IDisposable
{
	public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ShortTech", Guid.NewGuid().ToString("N"));

	public TempFolder()
	{
		Directory.CreateDirectory(Path);
	}

	public string File(string name)
	{
		return System.IO.Path.Combine(Path, name);
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(Path, recursive: true);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
