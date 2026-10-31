using System.Collections.Generic;
using System.Linq;

namespace ShortTech.Core.Documents;

public sealed record Table(List<List<List<Span>>> Rows, bool HeaderRow = true) : Block()
{
	public override string Text => string.Join("\n", Rows.Select((List<List<Span>> row) => string.Join("\t", row.Select((List<Span> cell) => string.Concat(cell.Select((Span s) => s.Text))))));
}
