using System;
using System.Threading;

namespace ShortTech.Core.Documents;

public static class Sta
{
	public static void Run(Action action)
	{
		if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
		{
			action();
			return;
		}
		Exception failure = null;
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				failure = ex;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		thread.Join();
		if (failure == null)
		{
			return;
		}
		throw failure;
	}
}
