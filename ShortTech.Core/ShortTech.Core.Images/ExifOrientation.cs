using System;

namespace ShortTech.Core.Images;

internal static class ExifOrientation
{
	private static readonly ushort[] Clockwise = new ushort[9] { 0, 6, 7, 8, 5, 2, 3, 4, 1 };

	public static byte[]? TryRotate(byte[] jpeg)
	{
		if (jpeg.Length < 4 || jpeg[0] != byte.MaxValue || jpeg[1] != 216)
		{
			return null;
		}
		int num;
		for (int i = 2; i + 4 < jpeg.Length && jpeg[i] == byte.MaxValue; i += 2 + num)
		{
			byte b = jpeg[i + 1];
			num = (jpeg[i + 2] << 8) | jpeg[i + 3];
			bool little;
			if (b == 225 && num > 8 && ((ReadOnlySpan<byte>)jpeg.AsSpan(i + 4, 6)).SequenceEqual((ReadOnlySpan<byte>)new byte[6] { 69, 120, 105, 102, 0, 0 }))
			{
				int num2 = i + 10;
				little = jpeg[num2] == 73;
				int num3 = num2 + (int)U(num2 + 4);
				ushort num4 = U16(num3);
				for (int j = 0; j < num4; j++)
				{
					int num5 = num3 + 2 + j * 12;
					if (U16(num5) == 274)
					{
						ushort num6 = U16(num5 + 8);
						if ((num6 < 1 || num6 > 8) ? true : false)
						{
							return null;
						}
						byte[] array = (byte[])jpeg.Clone();
						ushort num7 = Clockwise[num6];
						if (little)
						{
							array[num5 + 8] = (byte)num7;
							array[num5 + 9] = 0;
						}
						else
						{
							array[num5 + 8] = 0;
							array[num5 + 9] = (byte)num7;
						}
						return array;
					}
				}
				return null;
			}
			if (b == 218)
			{
				break;
			}
			uint U(int at)
			{
				if (!little)
				{
					return (uint)((jpeg[at] << 24) | (jpeg[at + 1] << 16) | (jpeg[at + 2] << 8) | jpeg[at + 3]);
				}
				return (uint)(jpeg[at] | (jpeg[at + 1] << 8) | (jpeg[at + 2] << 16) | (jpeg[at + 3] << 24));
			}
			ushort U16(int at)
			{
				if (!little)
				{
					return (ushort)((jpeg[at] << 8) | jpeg[at + 1]);
				}
				return (ushort)(jpeg[at] | (jpeg[at + 1] << 8));
			}
		}
		return null;
	}
}
