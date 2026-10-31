using System.Collections.Generic;

namespace ShortTech.Core.Documents;

public static class DocumentTargets
{
	public static readonly IReadOnlyList<DocumentTarget> All = new global::_003C_003Ez__ReadOnlyArray<DocumentTarget>(new DocumentTarget[8]
	{
		DocumentTarget.Pdf,
		DocumentTarget.Docx,
		DocumentTarget.Epub,
		DocumentTarget.Md,
		DocumentTarget.Txt,
		DocumentTarget.Rtf,
		DocumentTarget.Html,
		DocumentTarget.Odt
	});

	public static string Ext(this DocumentTarget target)
	{
		return target.ToString().ToLowerInvariant();
	}

	public static string Title(this DocumentTarget target)
	{
		if (target != DocumentTarget.Md)
		{
			return target.ToString().ToUpperInvariant();
		}
		return "MD";
	}
}
