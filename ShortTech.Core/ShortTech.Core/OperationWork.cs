using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShortTech.Core;

public delegate Task<IReadOnlyList<string>> OperationWork(IReadOnlyList<string> files, OperationContext context);
