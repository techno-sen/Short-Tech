using System;
using System.Runtime.InteropServices;

namespace ShortTech.App;

internal static partial class Native
{
	public delegate nint HookProc(int code, nint wParam, nint lParam);

	public struct POINT
	{
		public int X;

		public int Y;
	}

	public struct MSLLHOOKSTRUCT
	{
		public POINT Point;

		public uint MouseData;

		public uint Flags;

		public uint Time;

		public nint ExtraInfo;
	}

	public struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public struct MONITORINFO
	{
		public int Size;

		public RECT Monitor;

		public RECT Work;

		public uint Flags;
	}

	public const int WH_MOUSE_LL = 14;

	public const int WM_LBUTTONDOWN = 513;

	public const int WM_LBUTTONUP = 514;

	public const int WM_MOUSEMOVE = 512;

	public const int VK_SHIFT = 16;

	public const int VK_CONTROL = 17;

	public const int VK_MENU = 18;

	public const int VK_LBUTTON = 1;

	public const int GWL_EXSTYLE = -20;

	public const int WS_EX_NOACTIVATE = 134217728;

	public const int WS_EX_TOOLWINDOW = 128;

	public const int WS_EX_TOPMOST = 8;

	public const int WS_EX_TRANSPARENT = 32;

	public const int SM_CXDRAG = 68;

	public const int SM_CYDRAG = 69;

	public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	public const int DWMWCP_ROUND = 2;

	public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

	public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

	public const uint SND_MEMORY = 4u;

	public const uint SND_ASYNC = 1u;

	public const uint SND_NODEFAULT = 2u;

	[LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
	public static partial nint SetWindowsHookEx(int idHook, HookProc proc, nint module, uint threadId);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool UnhookWindowsHookEx(nint hook);

	[LibraryImport("user32.dll")]
	public static partial nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

	[LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
	public static partial nint GetModuleHandle(string? name);

	[LibraryImport("user32.dll")]
	public static partial short GetAsyncKeyState(int key);

	public static bool IsDown(int key)
	{
		return (GetAsyncKeyState(key) & 0x8000) != 0;
	}

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetCursorPos(out POINT point);

	[LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	public static partial nint GetWindowLongPtr(nint window, int index);

	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	public static partial nint SetWindowLongPtr(nint window, int index, nint value);

	[LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
	public static partial nint FindWindow(string? className, string? title);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool IsWindowVisible(nint window);

	[LibraryImport("user32.dll")]
	public static partial int GetSystemMetrics(int index);

	[LibraryImport("dwmapi.dll")]
	public static partial int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

	[LibraryImport("winmm.dll", EntryPoint = "PlaySoundW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool PlaySound(byte[] sound, nint module, uint flags);

	[LibraryImport("user32.dll")]
	public static partial nint MonitorFromPoint(POINT point, uint flags);

	[LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool GetMonitorInfo(nint monitor, ref MONITORINFO info);

	public static RECT WorkArea(int x, int y)
	{
		MONITORINFO info = new MONITORINFO
		{
			Size = Marshal.SizeOf<MONITORINFO>()
		};
		GetMonitorInfo(MonitorFromPoint(new POINT
		{
			X = x,
			Y = y
		}, 2u), ref info);
		return info.Work;
	}

	public static bool ShellDragInProgress()
	{
		nint num = FindWindow("SysDragImage", null);
		if (num != IntPtr.Zero)
		{
			return IsWindowVisible(num);
		}
		return false;
	}

	public static void MakeToolWindow(nint window, bool clickThrough = false)
	{
		long num = GetWindowLongPtr(window, -20);
		num |= 0x8000088;
		num = ((!clickThrough) ? (num & -33) : (num | 0x20));
		SetWindowLongPtr(window, -20, new IntPtr(num));
	}

	public static void RoundCorners(nint window)
	{
		int value = 2;
		DwmSetWindowAttribute(window, 33, ref value, 4);
	}
}
