namespace ShortTech.Core.Media;

public sealed record MediaInfo
{
	public double Duration { get; init; }

	public string? VideoCodec { get; init; }

	public string? AudioCodec { get; init; }

	public int AudioBits { get; init; }

	public int AudioChannels { get; init; }

	public bool IsHighDepth { get; init; }

	public int Width { get; init; }

	public int Height { get; init; }

	public double FrameRate { get; init; }

	public long FileSize { get; init; }

	public bool HasVideo => VideoCodec != null;

	public bool HasAudio => AudioCodec != null;

	public double Bitrate
	{
		get
		{
			if (!(Duration > 0.0))
			{
				return 0.0;
			}
			return (double)(FileSize * 8) / Duration;
		}
	}
}
