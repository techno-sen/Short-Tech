using System.Collections.Generic;
using System.Linq;

namespace ShortTech.App;

public static class Floats
{
	private static readonly List<FloatWindow> Open = new List<FloatWindow>();

	public static bool Any => Open.Count > 0;

	public static void Create(IReadOnlyList<string> files)
	{
		FloatWindow window = new FloatWindow(files);
		window.Closed += delegate
		{
			Open.Remove(window);
		};
		Open.Add(window);
		window.Show();
	}

	public static void ShowResults(IReadOnlyList<string> files)
	{
		FloatWindow floatWindow = Open.LastOrDefault();
		if (floatWindow != null)
		{
			floatWindow.Add(files);
			floatWindow.Activate();
		}
		else
		{
			Create(files);
		}
	}

	public static void CloseAll()
	{
		foreach (FloatWindow item in Open.ToList())
		{
			item.Close();
		}
	}
}
