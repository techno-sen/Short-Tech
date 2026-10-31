using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageMagick;

namespace ShortTech.Core.Images;

public static class ImageMetadata
{
	private static readonly HashSet<string> EditableExif = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"Artist", "Copyright", "ImageDescription", "Make", "Model", "Software", "DateTime", "DateTimeOriginal", "DateTimeDigitized", "UserComment",
		"LensModel", "BodySerialNumber"
	};

	public static List<MetadataField> Read(string path)
	{
		List<MetadataField> fields = new List<MetadataField>();
		using (MagickImage magickImage = new MagickImage(path))
		{
			FileInfo fileInfo = new FileInfo(path);
			Fact("Format", ImageFormat.Of(path)?.Name ?? FileKinds.Ext(path).ToUpperInvariant());
			Fact("Dimensions", $"{magickImage.Width} × {magickImage.Height}");
			Fact("File Size", Format.Bytes(fileInfo.Length));
			Fact("Bits Per Sample", magickImage.Depth.ToString(CultureInfo.InvariantCulture));
			Fact("Color Model", magickImage.ColorSpace.ToString().ToUpperInvariant().Replace("SRGB", "RGB"));
			IColorProfile colorProfile = magickImage.GetColorProfile();
			if (colorProfile != null)
			{
				Fact("Color Profile", colorProfile.Description ?? colorProfile.Model ?? "ICC");
			}
			if (magickImage.Density.X > 0.0)
			{
				Fact("Resolution", $"{Math.Round(magickImage.Density.ChangeUnits(DensityUnit.PixelsPerInch).X)} dpi");
			}
			IExifProfile exifProfile = magickImage.GetExifProfile();
			if (exifProfile != null)
			{
				foreach (IExifValue item in exifProfile.Values.OrderBy((IExifValue v) => v.Tag.ToString()))
				{
					string text = item.Tag.ToString();
					bool flag;
					switch (text)
					{
					case "MakerNote":
					case "PrintImageMatching":
					case "JPEGTables":
						flag = true;
						break;
					default:
						flag = false;
						break;
					}
					if (!flag && (item.DataType != ExifDataType.Undefined || !(text != "UserComment")))
					{
						string text2 = Describe(item);
						if (text2.Length != 0 && text2.Length <= 200)
						{
							string text3 = (text.StartsWith("GPS") ? "GPS" : "EXIF");
							fields.Add(new MetadataField
							{
								Group = text3,
								Name = Spaced(text),
								Original = text2,
								Value = text2,
								Tag = item.Tag,
								Editable = (EditableExif.Contains(text) && item.DataType == ExifDataType.String)
							});
						}
					}
				}
			}
			IIptcProfile iptcProfile = magickImage.GetIptcProfile();
			if (iptcProfile != null)
			{
				foreach (IIptcValue value in iptcProfile.Values)
				{
					fields.Add(new MetadataField
					{
						Group = "IPTC",
						Name = Spaced(value.Tag.ToString()),
						Original = value.Value,
						Value = value.Value,
						Iptc = value.Tag,
						Editable = true
					});
				}
			}
			return fields;
		}
		void Fact(string name, string value)
		{
			fields.Add(new MetadataField
			{
				Group = "File",
				Name = name,
				Original = value,
				Value = value
			});
		}
	}

	private static string Describe(IExifValue value)
	{
		object value2 = value.GetValue();
		if (value2 != null)
		{
			if (!(value2 is string text))
			{
				if (!(value2 is Rational rational))
				{
					if (!(value2 is Rational[] source))
					{
						if (!(value2 is byte[] source2))
						{
							if (value2 is Array source3)
							{
								return string.Join(", ", source3.Cast<object>());
							}
							return value2.ToString() ?? "";
						}
						return (value.Tag.ToString() == "UserComment") ? Encoding.UTF8.GetString(source2.Skip(8).ToArray()).Trim('\0', ' ') : "";
					}
					return string.Join(", ", source.Select((Rational r) => (r.Denominator != 0) ? ((double)r.Numerator / (double)r.Denominator).ToString("0.####", CultureInfo.InvariantCulture) : "0"));
				}
				return (rational.Denominator == 0) ? "" : ((double)rational.Numerator / (double)rational.Denominator).ToString("0.####", CultureInfo.InvariantCulture);
			}
			return text.Trim('\0', ' ');
		}
		return "";
	}

	private static string Spaced(string name)
	{
		return Regex.Replace(name, "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
	}

	public static async Task<string> Save(string path, IReadOnlyList<MetadataField> fields, bool removeAll, OperationContext context)
	{
		string output = Output.PathFor(path, FileKinds.Ext(path), removeAll ? "clean" : "edited");
		await Task.Run(delegate
		{
			using MagickImage magickImage = new MagickImage(path);
			IExifProfile exifProfile = magickImage.GetExifProfile() ?? new ExifProfile();
			IIptcProfile iptcProfile = magickImage.GetIptcProfile();
			if (removeAll)
			{
				IColorProfile colorProfile = magickImage.GetColorProfile();
				Orientation orientation = magickImage.Orientation;
				string text = FileKinds.Ext(path);
				if ((text == "jpg" || text == "jpeg") ? true : false)
				{
					File.WriteAllBytes(output, JpegSegments.Rewrite(File.ReadAllBytes(path), null, dropXmpAndIptc: true));
				}
				else
				{
					magickImage.Strip();
					if (colorProfile != null)
					{
						magickImage.SetProfile(colorProfile);
					}
					magickImage.Orientation = orientation;
					magickImage.Quality = 95u;
					magickImage.Write(output);
				}
			}
			else
			{
				foreach (MetadataField item in fields.Where((MetadataField f) => f.Changed))
				{
					ExifTag tag = item.Tag;
					if ((object)tag != null)
					{
						if (item.Removed)
						{
							exifProfile.RemoveValue(tag);
						}
						else if (tag is ExifTag<string> tag2)
						{
							exifProfile.SetValue(tag2, item.Value);
						}
					}
					else
					{
						IptcTag? iptc = item.Iptc;
						if (iptc.HasValue)
						{
							IptcTag valueOrDefault = iptc.GetValueOrDefault();
							if (iptcProfile != null)
							{
								iptcProfile.RemoveValue(valueOrDefault);
								if (!item.Removed)
								{
									iptcProfile.SetValue(valueOrDefault, item.Value);
								}
							}
						}
					}
				}
				if (fields.Any((MetadataField f) => f.Group == "GPS" && f.Removed))
				{
					foreach (ExifTag item2 in (from v in exifProfile.Values
						where v.Tag.ToString().StartsWith("GPS")
						select v.Tag).ToList())
					{
						exifProfile.RemoveValue(item2);
					}
				}
				string text = FileKinds.Ext(path);
				if ((text == "jpg" || text == "jpeg") ? true : false)
				{
					File.WriteAllBytes(output, JpegSegments.Rewrite(File.ReadAllBytes(path), exifProfile.ToByteArray(), dropXmpAndIptc: false));
				}
				else
				{
					magickImage.SetProfile(exifProfile);
					if (iptcProfile != null)
					{
						magickImage.SetProfile(iptcProfile);
					}
					magickImage.Quality = 95u;
					magickImage.Write(output);
				}
			}
		});
		context.Finish("Metadata saved");
		return output;
	}
}
