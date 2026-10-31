namespace ShortTech.Core.Documents;

public sealed record Picture(byte[] Data, string Extension, int PixelWidth = 0, int PixelHeight = 0, string Alt = "") : Block()
{
	public override string Text => "";

	public string Mime => Extension switch
	{
		"png" => "image/png", 
		"gif" => "image/gif", 
		"webp" => "image/webp", 
		"svg" => "image/svg+xml", 
		_ => "image/jpeg", 
	};
}
