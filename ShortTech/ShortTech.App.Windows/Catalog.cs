namespace ShortTech.App.Windows;

public static class Catalog
{
	public static void RegisterAll()
	{
		ToolWindows.Register("compress", CompressWindow.Make);
		ToolWindows.Register("metadata", MetadataWindow.Make);
		ToolWindows.Register("edit", EditPhotoWindow.Make);
		ToolWindows.Register("annotate", AnnotateWindow.Make);
		ToolWindows.Register("frame", FrameWindow.Make);
		ToolWindows.Register("crop", CropWindow.Make);
		ToolWindows.Register("redact", RedactWindow.Make);
		ToolWindows.Register("collage", CollageWindow.Make);
		ToolWindows.Register("organize", OrganizeWindow.Make);
		ToolWindows.Register("merge", OrderWindow.MergePdfs);
		ToolWindows.Register("trim", TrimWindow.Make);
		ToolWindows.Register("crop-video", CropVideoWindow.Make);
		ToolWindows.Register("speed", SpeedWindow.Make);
		ToolWindows.Register("frames", FramesWindow.Make);
		ToolWindows.Register("split", SplitWindow.Make);
		ToolWindows.Register("redact-video", RedactVideoWindow.Make);
		ToolWindows.Register("join", OrderWindow.JoinVideos);
		ToolWindows.Register("normalize", NormalizeWindow.Make);
		ToolWindows.Register("visualizer", VisualizerWindow.Make);
		ToolWindows.Register("channels", ChannelsWindow.Make);
		ToolWindows.Register("bleep", BleepWindow.Make);
		ToolWindows.Register("qr", QrWindow.Make);
		ToolWindows.Register("resize", ResizeWindow.Make);
		ToolWindows.Register("join-audio", OrderWindow.JoinAudio);
		ToolWindows.Register("rename", RenameWindow.Make);
		ToolWindows.Register("protect", ProtectWindow.Make);
	}
}
