using System.Collections.Generic;
using QRCoder;
using QRCoder.Exceptions;

namespace ShortTech.Core;

public sealed class QrMatrix
{
	public enum Correction
	{
		Low,
		Medium,
		Quartile,
		High
	}

	private readonly bool[] _dark;

	public int Size { get; }

	public IEnumerable<(int X, int Y)> FinderOrigins => new global::_003C_003Ez__ReadOnlyArray<(int, int)>(new(int, int)[3]
	{
		(0, 0),
		(Size - 7, 0),
		(0, Size - 7)
	});

	public static string Title(Correction correction)
	{
		return correction switch
		{
			Correction.Low => "Low · 7%", 
			Correction.Medium => "Medium · 15%", 
			Correction.Quartile => "Quartile · 25%", 
			_ => "High · 30%", 
		};
	}

	public bool IsDark(int x, int y)
	{
		if (x >= 0 && y >= 0 && x < Size && y < Size)
		{
			return _dark[y * Size + x];
		}
		return false;
	}

	public QrMatrix(int size, bool[] dark)
	{
		Size = size;
		_dark = dark;
	}

	public static QrMatrix? Encode(string text, Correction correction)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		try
		{
			QRCodeGenerator.ECCLevel eccLevel = correction switch
			{
				Correction.Low => QRCodeGenerator.ECCLevel.L, 
				Correction.Medium => QRCodeGenerator.ECCLevel.M, 
				Correction.Quartile => QRCodeGenerator.ECCLevel.Q, 
				_ => QRCodeGenerator.ECCLevel.H, 
			};
			using QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
			using QRCodeData qRCodeData = qRCodeGenerator.CreateQrCode(text, eccLevel, forceUtf8: true);
			int num = qRCodeData.ModuleMatrix.Count - 8;
			bool[] array = new bool[num * num];
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num; j++)
				{
					array[i * num + j] = qRCodeData.ModuleMatrix[i + 4][j + 4];
				}
			}
			return new QrMatrix(num, array);
		}
		catch (DataTooLongException)
		{
			return null;
		}
	}

	public bool IsFinder(int x, int y)
	{
		if (Size < 21)
		{
			return false;
		}
		int num = Size - 7;
		if ((x >= 7 || y >= 7) && (x < num || y >= 7))
		{
			if (x < 7)
			{
				return y >= num;
			}
			return false;
		}
		return true;
	}
}
