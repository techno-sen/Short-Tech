using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ShortTech.Core;

public static class Clipboard
{
	public static void CopyText(string text)
	{
		Sta(delegate
		{
			System.Windows.Clipboard.SetText(text);
		});
	}

	public static void CopyFiles(IEnumerable<string> paths)
	{
		StringCollection list = new StringCollection();
		list.AddRange(paths.ToArray());
		Sta(delegate
		{
			System.Windows.Clipboard.SetFileDropList(list);
		});
	}

	public static void CopyImage(BitmapSource image)
	{
		Sta(delegate
		{
			System.Windows.Clipboard.SetImage(image);
		});
	}

	public static string? Text()
	{
		string text = null;
		Sta(delegate
		{
			text = (System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null);
		});
		return text;
	}

	private static void Sta(Action action)
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			Retry(action);
			return;
		}
		Thread thread = new Thread((ThreadStart)delegate
		{
			Retry(action);
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
	}

	private static void Retry(Action action)
	{
		int num = 0;
		while (true)
		{
			try
			{
				action();
				break;
			}
			catch (COMException) when (num < 5)
			{
				Thread.Sleep(60);
			}
			num++;
		}
	}
}
