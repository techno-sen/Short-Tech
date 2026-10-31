using System.Collections.Generic;
using System.Linq;

namespace ShortTech.Core.Documents;

public sealed record Paragraph(List<Span> Spans, Align Align = Align.Start) : Block()
{
	public override string Text => string.Concat(Spans.Select((Span s) => s.Text));
}
