using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace ShortTech.Core.Documents;

public sealed class Doc
{
	public string Title { get; set; } = "Untitled";

	public List<Block> Blocks { get; } = new List<Block>();

	public string PlainText => string.Join(Environment.NewLine + Environment.NewLine, from block in Blocks
		select block.Text into text
		where text.Length > 0
		select text);

	public static Doc FromText(string text, string title = "Untitled")
	{
		Doc doc = new Doc
		{
			Title = title
		};
		string[] array = text.Replace("\r\n", "\n").Split("\n\n");
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim('\n');
			if (text2.Length != 0)
			{
				List<Block> blocks = doc.Blocks;
				int num = 1;
				List<Span> list = new List<Span>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = new Span(text2);
				blocks.Add(new Paragraph(list));
			}
		}
		return doc;
	}
}
