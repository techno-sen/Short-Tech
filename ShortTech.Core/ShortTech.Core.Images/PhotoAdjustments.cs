using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;

namespace ShortTech.Core.Images;

public sealed class PhotoAdjustments
{
	public static readonly IReadOnlyList<(string Title, IReadOnlyList<AdjustmentInfo> Items)> Sections = new global::_003C_003Ez__ReadOnlyArray<(string, IReadOnlyList<AdjustmentInfo>)>(new(string, IReadOnlyList<AdjustmentInfo>)[4]
	{
		("Light", new global::_003C_003Ez__ReadOnlyArray<AdjustmentInfo>(new AdjustmentInfo[8]
		{
			new AdjustmentInfo(Adjust.Exposure, "Exposure", -3.0, 3.0, " EV"),
			new AdjustmentInfo(Adjust.Brightness, "Brightness", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Contrast, "Contrast", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Highlights, "Highlights", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Shadows, "Shadows", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Whites, "Whites", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Blacks, "Blacks", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Gamma, "Gamma", -100.0, 100.0, "")
		})),
		("Color", new global::_003C_003Ez__ReadOnlyArray<AdjustmentInfo>(new AdjustmentInfo[5]
		{
			new AdjustmentInfo(Adjust.Temperature, "Temperature", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Tint, "Tint", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Saturation, "Saturation", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Vibrance, "Vibrance", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Hue, "Hue", -180.0, 180.0, "°")
		})),
		("Detail", new global::_003C_003Ez__ReadOnlyArray<AdjustmentInfo>(new AdjustmentInfo[4]
		{
			new AdjustmentInfo(Adjust.Clarity, "Clarity", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Dehaze, "Dehaze", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Sharpness, "Sharpness", 0.0, 100.0, ""),
			new AdjustmentInfo(Adjust.NoiseReduction, "Noise Reduction", 0.0, 100.0, "")
		})),
		("Effects", new global::_003C_003Ez__ReadOnlyArray<AdjustmentInfo>(new AdjustmentInfo[3]
		{
			new AdjustmentInfo(Adjust.Vignette, "Vignette", -100.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Grain, "Grain", 0.0, 100.0, ""),
			new AdjustmentInfo(Adjust.Fade, "Fade", 0.0, 100.0, "")
		}))
	});

	private readonly Dictionary<Adjust, double> _values = new Dictionary<Adjust, double>();

	public double this[Adjust key]
	{
		get
		{
			if (!_values.TryGetValue(key, out var value))
			{
				return 0.0;
			}
			return value;
		}
		set
		{
			if (Math.Abs(value) < 1E-09)
			{
				_values.Remove(key);
			}
			else
			{
				_values[key] = value;
			}
		}
	}

	public bool IsNeutral => _values.Count == 0;

	public PhotoAdjustments Copy()
	{
		PhotoAdjustments photoAdjustments = new PhotoAdjustments();
		foreach (var (key, value) in _values)
		{
			photoAdjustments._values[key] = value;
		}
		return photoAdjustments;
	}

	public void Apply(MagickImage image, double scale = 1.0)
	{
		if (IsNeutral)
		{
			return;
		}
		bool hasAlpha = image.HasAlpha;
		if (this[Adjust.Exposure] != 0.0)
		{
			image.Evaluate(Channels.RGB, EvaluateOperator.Multiply, Math.Pow(2.0, this[Adjust.Exposure]));
		}
		if (new Adjust[9]
		{
			Adjust.Brightness,
			Adjust.Contrast,
			Adjust.Highlights,
			Adjust.Shadows,
			Adjust.Whites,
			Adjust.Blacks,
			Adjust.Gamma,
			Adjust.Fade,
			Adjust.Dehaze
		}.Any((Adjust k) => this[k] != 0.0))
		{
			using MagickImage image2 = Curve();
			image.Clut(image2, PixelInterpolateMethod.Bilinear);
		}
		if (this[Adjust.Temperature] != 0.0 || this[Adjust.Tint] != 0.0)
		{
			double num = this[Adjust.Temperature] / 100.0;
			double num2 = this[Adjust.Tint] / 100.0;
			image.Evaluate(Channels.Red, EvaluateOperator.Multiply, 1.0 + num * 0.16);
			image.Evaluate(Channels.Blue, EvaluateOperator.Multiply, 1.0 - num * 0.16);
			image.Evaluate(Channels.Green, EvaluateOperator.Multiply, 1.0 - num2 * 0.12);
		}
		double num3 = this[Adjust.Saturation] + this[Adjust.Vibrance] * 0.6 + this[Adjust.Dehaze] * 0.15;
		if (num3 != 0.0 || this[Adjust.Hue] != 0.0)
		{
			image.Modulate(new Percentage(100), new Percentage(Math.Max(0.0, 100.0 + num3)), new Percentage(100.0 + this[Adjust.Hue] / 180.0 * 100.0));
		}
		if (this[Adjust.Clarity] != 0.0)
		{
			double num4 = Math.Max(4.0, (double)Math.Min(image.Width, image.Height) * 0.02);
			if (this[Adjust.Clarity] > 0.0)
			{
				image.UnsharpMask(0.0, num4 / 2.0, this[Adjust.Clarity] / 100.0 * 0.9, 0.02);
			}
			else
			{
				image.Blur(0.0, num4 / 6.0 * (0.0 - this[Adjust.Clarity]) / 100.0);
			}
		}
		if (this[Adjust.NoiseReduction] > 0.0)
		{
			image.Statistic(StatisticType.Median, (uint)Math.Max(1.0, Math.Round(this[Adjust.NoiseReduction] / 100.0 * 3.0 * scale + 1.0)), (uint)Math.Max(1.0, Math.Round(this[Adjust.NoiseReduction] / 100.0 * 3.0 * scale + 1.0)));
		}
		if (this[Adjust.Sharpness] > 0.0)
		{
			image.Sharpen(0.0, Math.Max(0.4, 1.2 * scale) * (0.5 + this[Adjust.Sharpness] / 100.0));
		}
		if (this[Adjust.Vignette] != 0.0)
		{
			using MagickImage magickImage = new MagickImage(MagickColors.White, image.Width, image.Height);
			double num5 = Math.Abs(this[Adjust.Vignette]) / 100.0;
			magickImage.BackgroundColor = ((this[Adjust.Vignette] > 0.0) ? MagickColors.Black : MagickColors.White);
			magickImage.Vignette(0.0, (double)Math.Max(image.Width, image.Height) * 0.35, 0, 0);
			magickImage.Evaluate(Channels.All, EvaluateOperator.Multiply, 1.0);
			using MagickImage magickImage2 = (MagickImage)image.Clone();
			magickImage2.Composite(magickImage, (this[Adjust.Vignette] > 0.0) ? CompositeOperator.Multiply : CompositeOperator.Screen);
			image.Composite(magickImage2, CompositeOperator.Blend, $"{num5 * 100.0}");
		}
		if (this[Adjust.Grain] > 0.0)
		{
			using MagickImage magickImage3 = new MagickImage(new MagickColor("#808080"), image.Width, image.Height);
			magickImage3.AddNoise(NoiseType.Gaussian, this[Adjust.Grain] / 100.0 * 1.4);
			magickImage3.ColorSpace = ColorSpace.Gray;
			image.Composite(magickImage3, CompositeOperator.SoftLight);
		}
		if (!hasAlpha && image.HasAlpha)
		{
			image.Alpha(AlphaAction.Off);
		}
	}

	private MagickImage Curve()
	{
		double num = this[Adjust.Brightness] / 100.0 * 0.25 - this[Adjust.Dehaze] / 100.0 * 0.04;
		double num2 = 1.0 + this[Adjust.Contrast] / 100.0 * 0.5 + this[Adjust.Dehaze] / 100.0 * 0.25;
		double num3 = this[Adjust.Highlights] / 100.0;
		double num4 = this[Adjust.Shadows] / 100.0;
		double val = this[Adjust.Whites] / 100.0 * 0.15;
		double num5 = this[Adjust.Blacks] / 100.0 * 0.15 + this[Adjust.Fade] / 100.0 * 0.18;
		double y = Math.Pow(2.0, (0.0 - this[Adjust.Gamma]) / 100.0);
		byte[] array = new byte[768];
		for (int i = 0; i < 256; i++)
		{
			double num6 = (double)i / 255.0;
			double num7 = Math.Max(0.0, num5);
			double num8 = 1.0 + Math.Min(0.0, val);
			double num9 = Math.Max(0.0, 0.0 - num5);
			double num10 = 1.0 - Math.Max(0.0, val);
			double num11 = Math.Clamp((num6 - num9) / Math.Max(1E-06, num10 - num9), 0.0, 1.0) * (num8 - num7) + num7;
			num11 += num4 * 0.35 * Math.Pow(1.0 - num11, 2.0) * num11 * 2.0;
			num11 += num3 * 0.35 * Math.Pow(num11, 2.0) * (1.0 - num11) * 2.0;
			num11 = Math.Pow(Math.Clamp(num11, 0.0, 1.0), y);
			num11 = (num11 - 0.5) * num2 + 0.5 + num;
			byte b = (byte)Math.Round(Math.Clamp(num11, 0.0, 1.0) * 255.0);
			array[i * 3] = (array[i * 3 + 1] = (array[i * 3 + 2] = b));
		}
		return new MagickImage(array, new MagickReadSettings
		{
			Width = 256u,
			Height = 1u,
			Format = MagickFormat.Rgb,
			Depth = 8u
		});
	}

	public static async Task<string> Save(string file, PhotoAdjustments adjustments, OperationContext context)
	{
		context.Report("Saving edited copy");
		ImageFormat format = ImageOps.EditedFormat(file);
		string output = Output.PathFor(file, format.Ext, "edited");
		await Task.Run(delegate
		{
			using MagickImage image = ImageEngine.Load(file);
			adjustments.Apply(image);
			ImageEngine.Save(image, format, output, 0.92);
		});
		return output;
	}
}
