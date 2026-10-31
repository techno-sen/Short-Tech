using System;
using System.Linq;
using System.Threading;
using System.Windows;
using ShortTech.Core;

namespace ShortTech.App;

public static class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		if (args.Length != 0 && args[0].StartsWith("--snapshot"))
		{
			int num = Array.IndexOf(args, "--lang");
			if (num >= 0 && num + 1 < args.Length)
			{
				L10n.Override(args[num + 1]);
			}
			return Snapshots.Run(new Application
			{
				ShutdownMode = ShutdownMode.OnExplicitShutdown
			}, args.Where((string a, int i) => a != "--lang" && (i == 0 || args[i - 1] != "--lang")).ToArray());
		}
		bool createdNew;
		using (new Mutex(initiallyOwned: true, "Local\\ShortTech.SingleInstance", out createdNew))
		{
			if (!createdNew)
			{
				return 0;
			}
			return new ShortTechApp().Run();
		}
	}
}
