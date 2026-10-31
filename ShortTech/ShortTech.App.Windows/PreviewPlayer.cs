using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using ShortTech.Core;

namespace ShortTech.App.Windows;

public sealed class PreviewPlayer
{
	private readonly MediaPlayer _player = new MediaPlayer();

	private CancellationTokenSource? _pending;

	public string? Playing { get; private set; }

	public event Action? Changed;

	public PreviewPlayer()
	{
		_player.MediaEnded += delegate
		{
			Stop();
		};
		_player.MediaFailed += delegate(object? _, ExceptionEventArgs e)
		{
			Stop();
			Hud.Shared.ShowMessage(L10n.T("Could not play the preview. ") + e.ErrorException?.Message, Hud.Style.Failure);
		};
	}

	public async void Toggle(string key, Func<Task<string>> render)
	{
		if (Playing == key)
		{
			Stop();
			return;
		}
		Stop();
		Playing = key;
		this.Changed?.Invoke();
		CancellationTokenSource cts = (_pending = new CancellationTokenSource());
		try
		{
			string uriString = await Task.Run(render);
			if (!cts.IsCancellationRequested && !(Playing != key))
			{
				_player.Open(new Uri(uriString));
				_player.Play();
			}
		}
		catch (Exception ex)
		{
			Playing = null;
			this.Changed?.Invoke();
			Hud.Shared.ShowMessage(L10n.T("Could not play the preview. ") + ex.Message, Hud.Style.Failure);
		}
	}

	public void Stop()
	{
		_pending?.Cancel();
		_player.Stop();
		_player.Close();
		Playing = null;
		this.Changed?.Invoke();
	}
}
