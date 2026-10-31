namespace ShortTech.Core.Images;

public sealed record FrameOptions
{
	public FrameBackground Background { get; init; } = new FrameBackground.Gradient(0);

	public double Padding { get; init; } = 160.0;

	public double Corners { get; init; } = 24.0;

	public double Shadow { get; init; } = 24.0;

	public FrameRatio Ratio { get; init; }
}
