using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ImageMagick;

namespace ShortTech.Core.Images;

public static class Collage
{
	public readonly record struct Cell(double X, double Y, double W, double H);

	public static List<Cell> Cells(int count, CollageOptions options, double width, double height, double spacing)
	{
		if (count <= 0)
		{
			return new List<Cell>();
		}
		Cell area = new Cell(0.0, 0.0, width, height);
		switch (options.Style)
		{
		case CollageStyle.Grid:
		{
			int num4 = Math.Min(Math.Max(1, options.Columns), count);
			return Grid(num4, (int)Math.Ceiling((double)count / (double)num4), area, count);
		}
		case CollageStyle.Row:
			return Grid(count, 1, area, count);
		case CollageStyle.Column:
			return Grid(1, count, area, count);
		default:
		{
			if (count == 1)
			{
				return Grid(1, 1, area, 1);
			}
			double num = height * 0.6;
			List<Cell> list = Grid(1, 1, new Cell(0.0, 0.0, width, num + spacing / 2.0), 1);
			List<Cell> list2 = Grid(count - 1, 1, new Cell(0.0, num - spacing / 2.0, width, height - num + spacing / 2.0), count - 1);
			int num2 = list.Count + list2.Count;
			List<Cell> list3 = new List<Cell>(num2);
			CollectionsMarshal.SetCount(list3, num2);
			Span<Cell> span = CollectionsMarshal.AsSpan(list3);
			int num3 = 0;
			Span<Cell> span2 = CollectionsMarshal.AsSpan(list);
			span2.CopyTo(span.Slice(num3, span2.Length));
			num3 += span2.Length;
			Span<Cell> span3 = CollectionsMarshal.AsSpan(list2);
			span3.CopyTo(span.Slice(num3, span3.Length));
			num3 += span3.Length;
			return list3;
		}
		}
		List<Cell> Grid(int columns, int rows, Cell cell, int n)
		{
			double w = (cell.W - spacing * (double)(columns + 1)) / (double)columns;
			double h = (cell.H - spacing * (double)(rows + 1)) / (double)rows;
			return (from i in Enumerable.Range(0, n)
				select new Cell(cell.X + spacing + (double)(i % columns) * (w + spacing), cell.Y + spacing + (double)(i / columns) * (h + spacing), w, h)).ToList();
		}
	}

	public static MagickImage Render(IReadOnlyList<MagickImage> images, CollageOptions options, double scale = 1.0)
	{
		double num = Math.Round((double)options.Width * scale);
		double num2 = Math.Round(num / options.Canvas.Ratio());
		MagickImage magickImage = new MagickImage(options.Background.Magick, (uint)num, (uint)num2);
		double spacing = options.Spacing * scale;
		double val = options.Corners * scale;
		List<Cell> list = Cells(images.Count, options, num, num2, spacing);
		for (int i = 0; i < images.Count && i < list.Count; i++)
		{
			Cell cell = list[i];
			uint num3 = (uint)Math.Max(1.0, Math.Round(cell.W));
			uint num4 = (uint)Math.Max(1.0, Math.Round(cell.H));
			using MagickImage magickImage2 = (MagickImage)images[i].Clone();
			using MagickImage magickImage3 = new MagickImage(MagickColors.Transparent, num3, num4);
			if (options.Fill)
			{
				using MagickImage image = Frames.Fill((MagickImage)magickImage2.Clone(), num3, num4);
				magickImage3.Composite(image, 0, 0, CompositeOperator.Over);
			}
			else
			{
				magickImage2.Resize(new MagickGeometry(num3, num4));
				magickImage3.Composite(magickImage2, (int)((num3 - magickImage2.Width) / 2), (int)((num4 - magickImage2.Height) / 2), CompositeOperator.Over);
			}
			using MagickImage image2 = Frames.Rounded(magickImage3, Math.Min(val, (double)Math.Min(num3, num4) / 2.0));
			magickImage.Composite(image2, (int)Math.Round(cell.X), (int)Math.Round(cell.Y), CompositeOperator.Over);
		}
		return magickImage;
	}

	public static async Task<IReadOnlyList<string>> Make(IReadOnlyList<string> files, CollageOptions options, OperationContext context)
	{
		context.Report("Creating collage");
		if (options.Style == CollageStyle.Grid && options.Columns == 2)
		{
			options = options with
			{
				Columns = ((files.Count <= 4) ? 2 : 3)
			};
		}
		string output = Output.Named("Collage.jpg", files[0]);
		await Task.Run(delegate
		{
			List<MagickImage> list = files.Select((string file) => ImageEngine.Load(file, options.Width)).ToList();
			try
			{
				using MagickImage image = Render(list, options);
				ImageEngine.Save(image, ImageFormat.Jpeg, output, 0.92);
			}
			finally
			{
				foreach (MagickImage item in list)
				{
					item.Dispose();
				}
			}
		});
		return new global::_003C_003Ez__ReadOnlySingleElementList<string>(output);
	}
}
