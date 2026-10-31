using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using ShortTech.Core;

namespace ShortTech.App;

public sealed class DragMonitor : IDisposable
{
	private readonly Native.HookProc _proc;

	private nint _hook;

	private readonly DispatcherTimer _timer;

	private Native.POINT _downAt;

	private bool _buttonDown;

	private bool _moved;

	private bool _shiftWas;

	private bool _ctrlWas;

	private bool _altWas;

	public bool IsDragging
	{
		get
		{
			if (_buttonDown)
			{
				return _moved;
			}
			return false;
		}
	}

	public event Action<ModifierKey, Point>? KeyDuringDrag;

	public event Action? DragEnded;

	public DragMonitor()
	{
		_proc = Hook;
		_timer = new DispatcherTimer(DispatcherPriority.Input)
		{
			Interval = TimeSpan.FromMilliseconds(16L)
		};
		_timer.Tick += delegate
		{
			Poll();
		};
	}

	public void Start()
	{
		if (_hook == IntPtr.Zero)
		{
			_hook = Native.SetWindowsHookEx(14, _proc, Native.GetModuleHandle(null), 0u);
		}
	}

	private nint Hook(int code, nint wParam, nint lParam)
	{
		if (code >= 0)
		{
			int num = (int)wParam;
			Native.MSLLHOOKSTRUCT mSLLHOOKSTRUCT = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
			switch (num)
			{
			case 513:
				_buttonDown = true;
				_moved = false;
				_downAt = mSLLHOOKSTRUCT.Point;
				_shiftWas = Native.IsDown(16);
				_ctrlWas = Native.IsDown(17);
				_altWas = Native.IsDown(18);
				_timer.Start();
				break;
			case 512:
				if (_buttonDown && !_moved)
				{
					int num2 = Math.Abs(mSLLHOOKSTRUCT.Point.X - _downAt.X);
					int num3 = Math.Abs(mSLLHOOKSTRUCT.Point.Y - _downAt.Y);
					if (num2 > Native.GetSystemMetrics(68) || num3 > Native.GetSystemMetrics(69))
					{
						_moved = true;
					}
				}
				break;
			case 514:
				if (_buttonDown)
				{
					Finish();
				}
				break;
			}
		}
		return Native.CallNextHookEx(_hook, code, wParam, lParam);
	}

	private void Finish()
	{
		_buttonDown = false;
		_moved = false;
		_timer.Stop();
		this.DragEnded?.Invoke();
	}

	private void Poll()
	{
		if (!Native.IsDown(1))
		{
			if (_buttonDown)
			{
				Finish();
			}
			return;
		}
		if (!_moved && Native.GetCursorPos(out var point) && (Math.Abs(point.X - _downAt.X) > Native.GetSystemMetrics(68) || Math.Abs(point.Y - _downAt.Y) > Native.GetSystemMetrics(69)))
		{
			_moved = true;
		}
		Check(16, ref _shiftWas, ModifierKey.Shift);
		Check(17, ref _ctrlWas, ModifierKey.Ctrl);
		Check(18, ref _altWas, ModifierKey.Alt);
	}

	private void Check(int key, ref bool was, ModifierKey modifier)
	{
		bool flag = Native.IsDown(key);
		if (flag && !was && File.Exists(Path.Combine(Settings.Folder, "debug-drag")))
		{
			Log.Write($"key {modifier} moved={_moved} shellDrag={Native.ShellDragInProgress()}");
		}
		if (flag && !was && _moved && Native.ShellDragInProgress() && Native.GetCursorPos(out var point))
		{
			this.KeyDuringDrag?.Invoke(modifier, new Point(point.X, point.Y));
		}
		was = flag;
	}

	public void Dispose()
	{
		_timer.Stop();
		if (_hook != IntPtr.Zero)
		{
			Native.UnhookWindowsHookEx(_hook);
		}
		_hook = IntPtr.Zero;
	}
}
