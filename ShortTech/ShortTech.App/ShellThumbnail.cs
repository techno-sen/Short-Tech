using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShortTech.App;

public static class ShellThumbnail
{
	[ComImport]
	[Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItemImageFactory
	{
		[PreserveSig]
		int GetImage(SIZE size, int flags, out nint bitmap);
	}

	private struct SIZE
	{
		public int Cx;

		public int Cy;
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	private static extern void SHCreateItemFromParsingName(string path, nint bindContext, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemImageFactory item);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint handle);

	public static ImageSource? For(string path, int pixels = 128)
	{
		try
		{
			SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out IShellItemImageFactory item);
			if (item.GetImage(new SIZE
			{
				Cx = pixels,
				Cy = pixels
			}, 0, out var bitmap) != 0)
			{
				return null;
			}
			try
			{
				BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				bitmapSource.Freeze();
				return bitmapSource;
			}
			finally
			{
				DeleteObject(bitmap);
			}
		}
		catch (Exception ex) when (((ex is COMException || ex is ArgumentException || ex is FileNotFoundException) ? 1 : 0) != 0)
		{
			return null;
		}
	}
}
