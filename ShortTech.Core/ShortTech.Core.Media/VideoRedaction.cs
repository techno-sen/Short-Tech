using System;

namespace ShortTech.Core.Media;

public sealed record VideoRedaction(double X, double Y, double W, double H, double Start, double End)
{
	public Guid Id { get; init; } = Guid.NewGuid();
}
