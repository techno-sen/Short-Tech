using System;

namespace ShortTech.Core.Pdf;

public sealed record PageSlot(int Source, int Rotation = 0)
{
	public Guid Id { get; } = Guid.NewGuid();
}
