using System;
using System.IO;
using ShortTech.Core;

namespace ShortTech.App;

public static class Sounds
{
	private static readonly byte[] TickSound = Wave(0.035, (double t) => Math.Min(1.0, t / 0.001) * Math.Exp((0.0 - t) / 0.0055) * (Math.Sin(Math.PI * 3800.0 * t) + 0.35 * Math.Sin(Math.PI * 7600.0 * t)) * 0.5);

	private static readonly byte[] DoneSound = Wave(0.32, delegate(double t)
	{
		return (Note(1046.5, 0.0) + Note(1568.0, 0.07)) * 0.32;
		double Note(double f, double at)
		{
			if (!(t < at))
			{
				return Math.Exp((0.0 - (t - at)) / 0.09) * Math.Min(1.0, (t - at) / 0.004) * Math.Sin(Math.PI * 2.0 * f * (t - at));
			}
			return 0.0;
		}
	});

	private static readonly byte[] FailSound = Wave(0.28, (double t) => Math.Min(1.0, t / 0.005) * Math.Exp((0.0 - t) / 0.08) * Math.Sin(Math.PI * 440.0 * t) * 0.45);

	public static void Tick()
	{
		Play(TickSound);
	}

	public static void Done()
	{
		Play(DoneSound);
	}

	public static void Failed()
	{
		Play(FailSound);
	}

	private static void Play(byte[] sound)
	{
		if (Settings.Current.PlaySounds)
		{
			Native.PlaySound(sound, IntPtr.Zero, 7u);
		}
	}

	private static byte[] Wave(double seconds, Func<double, double> sample)
	{
		int num = (int)(44100.0 * seconds);
		using MemoryStream memoryStream = new MemoryStream();
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write("RIFF"u8);
		binaryWriter.Write(36 + num * 2);
		binaryWriter.Write("WAVEfmt "u8);
		binaryWriter.Write(16);
		binaryWriter.Write((short)1);
		binaryWriter.Write((short)1);
		binaryWriter.Write(44100);
		binaryWriter.Write(88200);
		binaryWriter.Write((short)2);
		binaryWriter.Write((short)16);
		binaryWriter.Write("data"u8);
		binaryWriter.Write(num * 2);
		for (int i = 0; i < num; i++)
		{
			binaryWriter.Write((short)(Math.Clamp(sample((double)i / 44100.0), -1.0, 1.0) * 32767.0));
		}
		return memoryStream.ToArray();
	}
}
