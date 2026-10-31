using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using ShortTech.App.Wheel;
using ShortTech.Core;
using Microsoft.Win32;

namespace ShortTech.App;

public sealed class ShortTechApp : Application
{
	private readonly DragMonitor _drags = new DragMonitor();

	private WheelWindow? _wheel;

	private Tray? _tray;

	public ShortTechApp()
	{
		base.ShutdownMode = ShutdownMode.OnExplicitShutdown;
		base.DispatcherUnhandledException += delegate(object _, DispatcherUnhandledExceptionEventArgs e)
		{
			Log.Write($"unhandled: {e.Exception}");
			Hud.Shared.ShowMessage(e.Exception.Message, Hud.Style.Failure);
			e.Handled = true;
		};
		FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(L10n.IsArabic ? "ar" : "en-US")));
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		Log.Write($"started {typeof(ShortTechApp).Assembly.GetName().Version} on {Environment.OSVersion}");
		_wheel = new WheelWindow();
		_drags.KeyDuringDrag += delegate(ModifierKey key, Point point)
		{
			Settings current = Settings.Current;
			WheelMode? wheelMode = ((key == current.ConvertKey) ? new WheelMode?(WheelMode.Convert) : ((key == current.ToolsKey) ? new WheelMode?(WheelMode.Tools) : ((key == current.ActionsKey) ? new WheelMode?(WheelMode.Actions) : ((WheelMode?)null))));
			if (wheelMode.HasValue)
			{
				WheelMode valueOrDefault = wheelMode.GetValueOrDefault();
				_wheel.Show(valueOrDefault, point);
			}
		};
		_drags.DragEnded += delegate
		{
			DispatcherTimer timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(150L)
			};
			timer.Tick += delegate
			{
				timer.Stop();
				if (!Native.IsDown(1))
				{
					_wheel.HideWheel();
				}
			};
			timer.Start();
		};
		_drags.Start();
		FileActions.FloatHandler = delegate(IReadOnlyList<string> files)
		{
			Floats.Create(files);
			return Task.CompletedTask;
		};
		FileActions.ShareHandler = delegate(IReadOnlyList<string> files)
		{
			base.Dispatcher.Invoke(delegate
			{
				ShareUI.Share(files);
			});
			return Task.CompletedTask;
		};
		FileActions.ChooseFolder = (IReadOnlyList<string> files) => Task.FromResult(base.Dispatcher.Invoke(delegate
		{
			OpenFolderDialog openFolderDialog = new OpenFolderDialog
			{
				Title = L10n.T("Choose where to move " + ((files.Count == 1) ? Path.GetFileName(files[0]) : L10n.T($"{files.Count} items")))
			};
			return (openFolderDialog.ShowDialog() != true) ? null : openFolderDialog.FolderName;
		}));
		_tray = new Tray();
		if (!Settings.Current.HasSeenWelcome)
		{
			WelcomeWindow.Present();
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_drags.Dispose();
		_tray?.Dispose();
		base.OnExit(e);
	}
}
