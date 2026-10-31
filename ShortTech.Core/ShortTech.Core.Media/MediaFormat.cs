using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ShortTech.Core.Media;

public sealed record MediaFormat(string Name, string Ext, bool AudioOnly)
{
	public static readonly MediaFormat Mp4 = new MediaFormat("MP4", "mp4", AudioOnly: false);

	public static readonly MediaFormat Mov = new MediaFormat("MOV", "mov", AudioOnly: false);

	public static readonly MediaFormat Mkv = new MediaFormat("MKV", "mkv", AudioOnly: false);

	public static readonly MediaFormat WebM = new MediaFormat("WebM", "webm", AudioOnly: false);

	public static readonly MediaFormat Gif = new MediaFormat("GIF", "gif", AudioOnly: false);

	public static readonly MediaFormat Mp3 = new MediaFormat("MP3", "mp3", AudioOnly: true);

	public static readonly MediaFormat M4a = new MediaFormat("M4A", "m4a", AudioOnly: true);

	public static readonly MediaFormat Wav = new MediaFormat("WAV", "wav", AudioOnly: true);

	public static readonly MediaFormat Flac = new MediaFormat("FLAC", "flac", AudioOnly: true);

	public static readonly MediaFormat Aiff = new MediaFormat("AIFF", "aiff", AudioOnly: true);

	public static readonly MediaFormat Ogg = new MediaFormat("OGG", "ogg", AudioOnly: true);

	public static readonly MediaFormat Opus = new MediaFormat("Opus", "opus", AudioOnly: true);

	public static readonly IReadOnlyList<MediaFormat> Video = new global::_003C_003Ez__ReadOnlyArray<MediaFormat>(new MediaFormat[7] { Mp4, Mov, Mkv, WebM, Gif, Mp3, M4a });

	public static readonly IReadOnlyList<MediaFormat> Audio = new global::_003C_003Ez__ReadOnlyArray<MediaFormat>(new MediaFormat[7] { Mp3, M4a, Wav, Flac, Aiff, Ogg, Opus });

	[CompilerGenerated]
	private MediaFormat(MediaFormat original)
	{
		Name = original.Name;
		Ext = original.Ext;
		AudioOnly = original.AudioOnly;
	}
}
