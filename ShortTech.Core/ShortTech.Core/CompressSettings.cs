namespace ShortTech.Core;

public sealed record CompressSettings(CompressPreset Preset = CompressPreset.Balanced, int? ImageMaxPixels = null, int? VideoMaxHeight = null, CompressTarget? Target = null)
{
	public static CompressSettings Saved
	{
		get
		{
			Settings current = Settings.Current;
			return new CompressSettings(current.CompressPreset, (current.CompressImageMaxPixels > 0) ? new int?(current.CompressImageMaxPixels) : ((int?)null), (current.CompressVideoMaxHeight > 0) ? new int?(current.CompressVideoMaxHeight) : ((int?)null));
		}
	}
}
