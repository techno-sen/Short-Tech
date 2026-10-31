namespace ShortTech.Core;

public sealed record ShortTechOperation(string Id, string Title, string Icon, OperationWork Run)
{
	public string? Badge { get; init; }

	public string? Short { get; init; }

	public bool ProducesFiles { get; init; } = true;

	public string? Window { get; init; }

	public string WheelLabel => Badge ?? L10n.Upper(L10n.T(Short ?? Title));
}
