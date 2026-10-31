using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ShortTech.Core;
using WinRT;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ShortTech.App;

public static class ShareUI
{
	[ComImport]
	[Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IDataTransferManagerInterop
	{
		nint GetForWindow(nint window, ref Guid riid);

		void ShowShareUIForWindow(nint window);
	}

	private static readonly Guid DataTransferManagerId = new Guid(2781539995u, 34568, 18897, 141, 54, 103, 210, 90, 141, 160, 12);

	private static Window? _host;

	public static void Share(IReadOnlyList<string> files)
	{
		if (_host == null)
		{
			_host = new Window
			{
				Width = 1.0,
				Height = 1.0,
				WindowStyle = WindowStyle.None,
				ShowInTaskbar = false,
				ShowActivated = true,
				Left = -100.0,
				Top = -100.0,
				AllowsTransparency = true,
				Opacity = 0.0
			};
		}
		_host.Show();
		nint handle = new WindowInteropHelper(_host).Handle;
		IDataTransferManagerInterop dataTransferManagerInterop = DataTransferManager.As<IDataTransferManagerInterop>();
		Guid riid = DataTransferManagerId;
		MarshalInterface<DataTransferManager>.FromAbi(dataTransferManagerInterop.GetForWindow(handle, ref riid)).DataRequested += OnRequested;
		dataTransferManagerInterop.ShowShareUIForWindow(handle);
		void OnRequested(DataTransferManager sender, DataRequestedEventArgs args)
		{
			sender.DataRequested -= OnRequested;
			DataRequestDeferral deferral = args.Request.GetDeferral();
			args.Request.Data.Properties.Title = ((files.Count == 1) ? Path.GetFileName(files[0]) : L10n.T($"{files.Count} files"));
			Fill(args.Request.Data, files, deferral);
		}
	}

	private static async Task Fill(DataPackage data, IReadOnlyList<string> files, DataRequestDeferral deferral)
	{
		_ = 1;
		try
		{
			List<IStorageItem> items = new List<IStorageItem>();
			foreach (string file in files)
			{
				List<IStorageItem> list = items;
				IStorageItem item = ((!Directory.Exists(file)) ? ((IStorageItem)(await StorageFile.GetFileFromPathAsync(file))) : ((IStorageItem)(await StorageFolder.GetFolderFromPathAsync(file))));
				list.Add(item);
			}
			data.SetStorageItems(items);
		}
		finally
		{
			deferral.Complete();
		}
	}
}
