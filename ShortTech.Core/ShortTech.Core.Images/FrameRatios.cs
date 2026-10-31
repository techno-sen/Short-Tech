namespace ShortTech.Core.Images;

public static class FrameRatios
{
	public static string Title(this FrameRatio ratio)
	{
		return ratio switch
		{
			FrameRatio.Auto => "Auto", 
			FrameRatio.Square => "1:1", 
			FrameRatio.Wide => "16:9", 
			FrameRatio.Portrait => "4:5", 
			_ => "9:16", 
		};
	}

	public static double? Value(this FrameRatio ratio)
	{
		return ratio switch
		{
			FrameRatio.Square => 1.0, 
			FrameRatio.Wide => 1.7777777777777777, 
			FrameRatio.Portrait => 0.8, 
			FrameRatio.Story => 0.5625, 
			_ => null, 
		};
	}
}
