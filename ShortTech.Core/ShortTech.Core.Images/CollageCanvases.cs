namespace ShortTech.Core.Images;

public static class CollageCanvases
{
	public static string Title(this CollageCanvas canvas)
	{
		return canvas switch
		{
			CollageCanvas.Square => "Square · 1:1", 
			CollageCanvas.Portrait => "Portrait · 4:5", 
			CollageCanvas.Story => "Story · 9:16", 
			CollageCanvas.Landscape => "Landscape · 16:9", 
			_ => "Classic · 4:3", 
		};
	}

	public static double Ratio(this CollageCanvas canvas)
	{
		return canvas switch
		{
			CollageCanvas.Square => 1.0, 
			CollageCanvas.Portrait => 0.8, 
			CollageCanvas.Story => 0.5625, 
			CollageCanvas.Landscape => 1.7777777777777777, 
			_ => 1.3333333333333333, 
		};
	}
}
