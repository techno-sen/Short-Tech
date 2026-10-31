namespace ShortTech.Core.Media;

public static class VisualizerShapes
{
	public static string Title(this VisualizerShape shape)
	{
		return shape switch
		{
			VisualizerShape.Landscape => "Landscape · 16:9", 
			VisualizerShape.Portrait => "Portrait · 9:16", 
			_ => "Square · 1:1", 
		};
	}

	public static (int Width, int Height) Size(this VisualizerShape shape)
	{
		return shape switch
		{
			VisualizerShape.Landscape => (Width: 1920, Height: 1080), 
			VisualizerShape.Portrait => (Width: 1080, Height: 1920), 
			_ => (Width: 1080, Height: 1080), 
		};
	}
}
