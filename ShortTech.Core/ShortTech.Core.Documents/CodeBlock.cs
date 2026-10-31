namespace ShortTech.Core.Documents;

public sealed record CodeBlock(string Code) : Block()
{
	public override string Text => Code;
}
