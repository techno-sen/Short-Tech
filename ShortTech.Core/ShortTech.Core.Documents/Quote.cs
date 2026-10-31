using System.Collections.Generic;
using System.Linq;

namespace ShortTech.Core.Documents;

public sealed record Quote(List<Span> Spans) : Block()
{
	public override string Text => string.Concat(Spans.Select((Span s) => s.Text));
}
