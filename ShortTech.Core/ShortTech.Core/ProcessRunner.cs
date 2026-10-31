using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShortTech.Core;

public static class ProcessRunner
{
	public sealed record Result(int ExitCode, string Output, string Error);

	public static string? Find(string name)
	{
		string text = (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : (name + ".exe"));
		string text2 = Path.Combine(AppContext.BaseDirectory, "tools", text);
		if (File.Exists(text2))
		{
			return text2;
		}
		string[] array = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
		foreach (string text3 in array)
		{
			try
			{
				string text4 = Path.Combine(text3.Trim(), text);
				if (File.Exists(text4))
				{
					return text4;
				}
			}
			catch (ArgumentException)
			{
			}
		}
		return null;
	}

	public static string Require(string name)
	{
		return Find(name) ?? throw new ShortTechException(name + " is not installed. Install it with: winget install " + name);
	}

	public static async Task<Result> Run(string tool, IEnumerable<string> arguments, Action<string>? onLine = null, CancellationToken cancellation = default(CancellationToken), string? input = null)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(tool)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			RedirectStandardInput = (input != null),
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};
		foreach (string argument in arguments)
		{
			processStartInfo.ArgumentList.Add(argument);
		}
		using Process process = new Process
		{
			StartInfo = processStartInfo
		};
		StringBuilder output = new StringBuilder();
		StringBuilder error = new StringBuilder();
		process.OutputDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				lock (output)
				{
					output.AppendLine(e.Data);
				}
				onLine?.Invoke(e.Data);
			}
		};
		process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (e.Data == null)
			{
				return;
			}
			lock (error)
			{
				error.AppendLine(e.Data);
			}
		};
		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		if (input != null)
		{
			await process.StandardInput.WriteAsync(input);
			process.StandardInput.Close();
		}
		try
		{
			await process.WaitForExitAsync(cancellation);
		}
		catch (OperationCanceledException)
		{
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch (InvalidOperationException)
			{
			}
			throw;
		}
		process.WaitForExit();
		return new Result(process.ExitCode, output.ToString(), error.ToString());
	}
}
