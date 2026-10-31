namespace ShortTech.Core;

public static class CompressPresets
{
	public static string Title(this CompressPreset preset)
	{
		return preset switch
		{
			CompressPreset.Light => "Light", 
			CompressPreset.Balanced => "Balanced", 
			_ => "Strong", 
		};
	}

	public static string Detail(this CompressPreset preset)
	{
		return preset switch
		{
			CompressPreset.Light => "Better visual quality with moderate file-size savings.", 
			CompressPreset.Balanced => "Smaller files with less quality loss.", 
			_ => "Smaller files with more quality loss.", 
		};
	}

	public static double ImageQuality(this CompressPreset preset)
	{
		return preset switch
		{
			CompressPreset.Light => 0.85, 
			CompressPreset.Balanced => 0.7, 
			_ => 0.5, 
		};
	}

	public static double MediaShare(this CompressPreset preset)
	{
		return preset switch
		{
			CompressPreset.Light => 0.65, 
			CompressPreset.Balanced => 0.45, 
			_ => 0.3, 
		};
	}
}
