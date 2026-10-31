using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ShortTech.Core;
using H.NotifyIcon;

namespace ShortTech.App;

public sealed class Tray : IDisposable
{
	private const string InfoGlyph = "\uE946";

	private readonly TaskbarIcon _icon;

	public Tray()
	{
		Settings current = Settings.Current;
		_icon = new TaskbarIcon
		{
			ToolTipText = L10n.T($"Drag files, then press {Key(current.ConvertKey)}, {Key(current.ToolsKey)} or {Key(current.ActionsKey)}"),
			IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/ShortTech.ico")),
			ContextMenu = Menu(),
			NoLeftClickDelay = true
		};
		_icon.TrayLeftMouseUp += delegate
		{
			_icon.ContextMenu.IsOpen = true;
		};
		_icon.ForceCreate(enablesEfficiencyMode: false);
	}

	public static string Key(ModifierKey key)
	{
		return key switch
		{
			ModifierKey.Shift => "Shift", 
			ModifierKey.Ctrl => "Ctrl", 
			ModifierKey.Alt => "Alt", 
			_ => "—", 
		};
	}

	private static ContextMenu Menu()
	{
		ContextMenu obj = new ContextMenu
		{
			FlowDirection = Theme.Direction,
			FontFamily = Theme.Font
		};
		MenuItem newItem = new MenuItem
		{
			Header = L10n.T("Short Tech — drag files, then press a modifier key"),
			IsEnabled = false
		};
		obj.Items.Add(newItem);
		obj.Items.Add(new Separator());
		obj.Items.Add(Item("New Float", delegate
		{
			Floats.Create(Array.Empty<string>());
		}, "\ue840"));
		MenuItem closeFloats = Item("Close All Floats", Floats.CloseAll, "\ue8bb");
		obj.Items.Add(closeFloats);
		obj.Items.Add(Item("Create QR Code…", delegate
		{
			ToolWindows.Open("qr", Array.Empty<string>());
		}, "\ued14"));
		obj.Items.Add(new Separator());
		obj.Items.Add(Item("Welcome Guide…", WelcomeWindow.Present, "\ue897"));
		obj.Items.Add(Item("Settings…", SettingsWindow.Present, "\ue713"));
		obj.Items.Add(Item("About Short Tech…", SettingsWindow.PresentAbout, InfoGlyph));
		obj.Items.Add(new Separator());
		obj.Items.Add(Item("Exit Short Tech", delegate
		{
			Application.Current.Shutdown();
		}, "\ue7e8"));
		obj.Opened += delegate
		{
			closeFloats.IsEnabled = Floats.Any;
		};
		return obj;
		static MenuItem Item(string title, Action action, string? icon = null)
		{
			MenuItem menuItem = new MenuItem
			{
				Header = L10n.T(title)
			};
			if (icon != null)
			{
				menuItem.Icon = new TextBlock
				{
					Text = icon,
					FontFamily = Theme.Icons,
					FontSize = 14.0
				};
			}
			menuItem.Click += delegate
			{
				action();
			};
			return menuItem;
		}
	}

	public void Dispose()
	{
		_icon.Dispose();
	}
}
