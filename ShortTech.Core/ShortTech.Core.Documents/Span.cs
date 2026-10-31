namespace ShortTech.Core.Documents;

public sealed record Span(string Text, bool Bold = false, bool Italic = false, bool Underline = false, string? Link = null, bool Code = false);
