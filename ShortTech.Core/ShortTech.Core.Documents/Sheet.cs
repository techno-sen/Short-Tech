using System.Collections.Generic;

namespace ShortTech.Core.Documents;

public sealed record Sheet(string Name, List<List<object?>> Rows);
