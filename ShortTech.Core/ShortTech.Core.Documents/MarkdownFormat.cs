using Markdig;

namespace ShortTech.Core.Documents;

public static class MarkdownFormat
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

	public static Doc Read(string markdown, string title)
	{
		return HtmlReader.Read(Markdown.ToHtml(markdown, Pipeline), title);
	}
}
