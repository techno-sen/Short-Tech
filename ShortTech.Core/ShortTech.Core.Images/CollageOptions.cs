namespace ShortTech.Core.Images;

public sealed record CollageOptions
{
	public CollageStyle Style { get; init; }

	public int Columns { get; init; } = 2;

	public CollageCanvas Canvas { get; init; }

	public int Width { get; init; } = 2048;

	public bool Fill { get; init; } = true;

	public double Spacing { get; init; } = 16.0;

	public double Corners { get; init; }

	public Rgb Background { get; init; } = new Rgb(1.0, 1.0, 1.0);
}
