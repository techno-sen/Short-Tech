# Short Tech

File tools for Windows, one gesture away. Start dragging any file, press **Shift**, **Ctrl** or **Alt**, and a wheel opens at the cursor. Drop the file on what you need.

| Key | Wheel | What it offers |
| --- | --- | --- |
| Shift | Convert | Images, video, audio, PDF, documents, spreadsheets, slides, subtitles and archives to other formats |
| Ctrl | Tools | Depends on the file: resize, compress, crop, trim, change speed, join, redact, annotate, read text, lock a PDF with a password and more |
| Alt | Actions | Share, Float, copy, move, rename in bulk, copy path, open a terminal |

Files are processed on your computer and are never uploaded. The interface is available in English and Arabic.

## Build from source

You need Windows 10 (1809 or later, x64) and the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```
dotnet build ShortTech.sln -c Release
```

**FFmpeg is not in this repository** because the file is larger than GitHub's limit. Video and audio tools need it, so either put `ffmpeg.exe` in `ShortTech/tools/` before building, or have it on your `PATH`. The exact build used by Short Tech is listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Build the installer

With `ffmpeg.exe` in place and [Inno Setup 6](https://jrsoftware.org/isinfo.php) installed:

```
dotnet publish ShortTech/ShortTech.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o publish/ShortTech
dotnet publish shorttech-cli/shorttech-cli.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o publish/ShortTech
iscc installer/ShortTech.iss
```

The installer is written to `installer/Output/`. It includes its own copy of .NET, so the target PC does not need .NET installed.

## License

Short Tech is free software under the [GNU General Public License v3](LICENSE.txt) and comes with no warranty. Origin and third-party components are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Developed by [Techno Sen](https://www.techno-sen.com).
