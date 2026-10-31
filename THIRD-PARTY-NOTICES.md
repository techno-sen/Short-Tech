# Short Tech — licenses and third-party software

Short Tech is developed by Techno Sen (https://www.techno-sen.com) and is licensed under the GNU General Public
License v3.0 (see `LICENSE.txt`). It is a modified version of Adawat by Motalim, also licensed under the GPL v3;
Techno Sen modified it in 2026. Complete source code: https://github.com/techno-sen/Short-Tech

## FFmpeg 8.1 — GPL v3

`tools\ffmpeg.exe` is the `gpl` build of FFmpeg `n8.1.3-18-g46d899d7a0` from
[BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds), release `autobuild-2026-10-18-13-01`. It is configured
with `--enable-gpl --enable-version3` and without nonfree components, so the binary as a whole is covered by the
[GNU GPL v3](https://www.gnu.org/licenses/gpl-3.0.html). Its license text ships next to it as
`tools\ffmpeg-LICENSE.txt`. It runs as a separate program; Short Tech calls it and does not link to it.

**Corresponding source.** FFmpeg at commit `46d899d7a0` (https://github.com/FFmpeg/FFmpeg), and the build scripts
that pin every bundled library and its version at the release tag above
(https://github.com/BtbN/FFmpeg-Builds/tree/autobuild-2026-10-18-13-01).

```
76f92392602b95351f4f057b8f6d74815e8c6947a2495c55193bd92f0ea7a2bd  ffmpeg-n8.1.3-18-g46d899d7a0-win64-gpl-8.1.zip
```

## Libraries

| Component | License | Source |
| --- | --- | --- |
| .NET runtime and WPF | MIT | https://github.com/dotnet/runtime, https://github.com/dotnet/wpf |
| Magick.NET 15 (with ImageMagick and its image libraries) | Apache 2.0 (ImageMagick License) | https://github.com/dlemstra/Magick.NET |
| PDFtoImage 5 | MIT | https://github.com/sungaila/PDFtoImage |
| PDFium (bblanchon.PDFium) | Apache 2.0 / BSD 3-Clause | https://github.com/bblanchon/pdfium-binaries |
| SkiaSharp | MIT | https://github.com/mono/SkiaSharp |
| PDFsharp 6.2.4 | MIT | https://github.com/empira/PDFsharp |
| PdfPig | Apache 2.0 | https://github.com/UglyToad/PdfPig |
| HtmlAgilityPack | MIT | https://github.com/zzzprojects/html-agility-pack |
| DocumentFormat.OpenXml | MIT | https://github.com/dotnet/Open-XML-SDK |
| ClosedXML, ClosedXML.Parser | MIT | https://github.com/ClosedXML/ClosedXML |
| ExcelNumberFormat, RBush | MIT | https://github.com/andersnm/ExcelNumberFormat, https://github.com/viceroypenguin/RBush |
| SixLabors.Fonts | Apache 2.0 | https://github.com/SixLabors/Fonts |
| ExcelDataReader | MIT | https://github.com/ExcelDataReader/ExcelDataReader |
| Markdig | BSD 2-Clause | https://github.com/xoofx/markdig |
| SharpCompress | MIT | https://github.com/adamhathcock/sharpcompress |
| QRCoder | MIT | https://github.com/codebude/QRCoder |
| ZXing.Net | Apache 2.0 | https://github.com/micjahn/ZXing.Net |
| H.NotifyIcon | MIT | https://github.com/HavenDV/H.NotifyIcon |
| Microsoft Edge WebView2 SDK | BSD 3-Clause | https://www.nuget.org/packages/Microsoft.Web.WebView2 |
| WinRT.Runtime (C#/WinRT) | MIT | https://github.com/microsoft/CsWinRT |
| Microsoft.Windows.SDK.NET (Windows API projection, a system library) | Windows SDK License | https://aka.ms/WinSDKLicenseURL |

The installer is made with [Inno Setup](https://jrsoftware.org/isinfo.php), whose license allows installers for
any program. Copyright for each component belongs to its authors; their full license texts are in their sources.
