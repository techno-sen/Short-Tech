using System;
using System.IO;

namespace ShortTech.Core.Images;

internal static class JpegSegments
{
	public static byte[] Rewrite(byte[] jpeg, byte[]? exif, bool dropXmpAndIptc)
	{
		if (jpeg.Length < 4 || jpeg[0] != byte.MaxValue || jpeg[1] != 216)
		{
			throw new ShortTechException("This file type's metadata cannot be edited here.");
		}
		MemoryStream output = new MemoryStream();
		bool wroteExif;
		try
		{
			output.Write(jpeg, 0, 2);
			int i = 2;
			wroteExif = false;
			int num;
			for (; i + 4 <= jpeg.Length && jpeg[i] == byte.MaxValue; i += num + 2)
			{
				byte b = jpeg[i + 1];
				if (b == 218)
				{
					break;
				}
				num = (jpeg[i + 2] << 8) | jpeg[i + 3];
				Span<byte> span = jpeg.AsSpan(i, num + 2);
				bool flag = b == 225 && num > 8 && ((ReadOnlySpan<byte>)jpeg.AsSpan(i + 4, 6)).SequenceEqual((ReadOnlySpan<byte>)new byte[6] { 69, 120, 105, 102, 0, 0 });
				bool flag2 = b == 225 && !flag;
				bool flag3 = b == 237;
				bool flag4 = b == 254;
				if (b != 224)
				{
					WriteExif();
				}
				if (!flag && (!dropXmpAndIptc || !(flag2 || flag3 || flag4)))
				{
					output.Write(span);
				}
			}
			WriteExif();
			output.Write(jpeg, i, jpeg.Length - i);
			return output.ToArray();
		}
		finally
		{
			if (output != null)
			{
				((IDisposable)output).Dispose();
			}
		}
		void WriteExif()
		{
			if (!wroteExif && exif != null && exif.Length != 0)
			{
				wroteExif = true;
				byte[]? array4;
				if (!((ReadOnlySpan<byte>)exif.AsSpan()).StartsWith((ReadOnlySpan<byte>)new byte[6] { 69, 120, 105, 102, 0, 0 }))
				{
					byte[] array = ((ReadOnlySpan<byte>)new byte[6] { 69, 120, 105, 102, 0, 0 }).ToArray();
					byte[] array2 = exif;
					int num2 = 0;
					byte[] array3 = new byte[array.Length + array2.Length];
					ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(array);
					readOnlySpan.CopyTo(new Span<byte>(array3).Slice(num2, readOnlySpan.Length));
					num2 += readOnlySpan.Length;
					ReadOnlySpan<byte> readOnlySpan2 = new ReadOnlySpan<byte>(array2);
					readOnlySpan2.CopyTo(new Span<byte>(array3).Slice(num2, readOnlySpan2.Length));
					num2 += readOnlySpan2.Length;
					array4 = array3;
				}
				else
				{
					array4 = exif;
				}
				byte[] array5 = array4;
				output.WriteByte(byte.MaxValue);
				output.WriteByte(225);
				int num3 = array5.Length + 2;
				output.WriteByte((byte)(num3 >> 8));
				output.WriteByte((byte)num3);
				output.Write(array5);
			}
		}
	}
}
