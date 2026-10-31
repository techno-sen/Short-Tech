using System;

namespace ShortTech.Core;

public sealed class ShortTechException : Exception
{
	public override string Message => L10n.T(base.Message);

	public ShortTechException(string message)
		: base(message)
	{
	}
}
