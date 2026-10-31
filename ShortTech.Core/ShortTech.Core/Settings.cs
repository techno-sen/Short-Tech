using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShortTech.Core;

public sealed class Settings
{
	public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShortTech");

	private static readonly string FilePath = Path.Combine(Folder, "settings.json");

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	public static Settings Current { get; } = Load();

	public ModifierKey ConvertKey { get; set; }

	public ModifierKey ToolsKey { get; set; } = ModifierKey.Ctrl;

	public ModifierKey ActionsKey { get; set; } = ModifierKey.Alt;

	public OutputLocation OutputLocation { get; set; }

	public bool ShowResultsInFloat { get; set; }

	public bool PlaySounds { get; set; } = true;

	public double ImageQuality { get; set; } = 0.85;

	public int ResizeMaxPixels { get; set; } = 2048;

	public VideoQuality VideoQuality { get; set; } = VideoQuality.Balanced;

	public CompressPreset CompressPreset { get; set; } = CompressPreset.Balanced;

	public int CompressImageMaxPixels { get; set; }

	public int CompressVideoMaxHeight { get; set; }

	public bool HasSeenWelcome { get; set; }

	public string Language { get; set; } = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar") ? "ar" : "en";

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Folder);
			File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
		}
		catch (IOException)
		{
		}
	}

	private static Settings Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Json) ?? new Settings();
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is JsonException) ? 1 : 0) != 0)
		{
		}
		return new Settings();
	}
}
