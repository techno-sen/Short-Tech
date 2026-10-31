using System.Collections.Generic;
using System.Linq;

namespace ShortTech.Core.Documents;

public sealed record ListItem(List<Span> Spans, bool Ordered, int Depth = 0) : Block()
{
	public override string Text => string.Concat(Spans.Select((Span s) => s.Text));
}
