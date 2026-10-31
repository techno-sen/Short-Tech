namespace ShortTech.Core;

public abstract record CompressTarget
{
	public sealed record Bytes(long Value) : CompressTarget();

	public sealed record Reduction(double Share) : CompressTarget();

	public long For(long original)
	{
		if (!(this is Bytes { Value: var value }))
		{
			if (this is Reduction reduction)
			{
				return (long)((double)original * (1.0 - reduction.Share));
			}
			return original;
		}
		return value;
	}
}
