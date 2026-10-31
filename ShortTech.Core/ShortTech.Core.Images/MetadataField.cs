using ImageMagick;

namespace ShortTech.Core.Images;

public sealed class MetadataField
{
	public required string Group { get; init; }

	public required string Name { get; init; }

	public required string Original { get; init; }

	public string Value { get; set; } = "";

	public bool Editable { get; init; }

	public bool Removed { get; set; }

	public ExifTag? Tag { get; init; }

	public IptcTag? Iptc { get; init; }

	public bool Changed
	{
		get
		{
			if (!Removed)
			{
				return Value != Original;
			}
			return true;
		}
	}
}
