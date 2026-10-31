using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ShortTech.Core;
using ShortTech.Core.Media;

namespace ShortTech.App.Windows;

public sealed class MediaSession : IDisposable
{
	private readonly DispatcherTimer _timer = new DispatcherTimer
	{
		Interval = TimeSpan.FromMilliseconds(33L)
	};

	private readonly DispatcherTimer _stillTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromMilliseconds(90L)
	};

	private double? _stopAt;

	private bool _ready;

	private bool _disposed;

	public string File { get; }

	public MediaInfo Info { get; }

	public double Duration => Info.Duration;

	public double FrameStep { get; }

	public MediaElement? Player { get; private set; }

	public Image Still { get; } = new Image
	{
		Stretch = Stretch.Uniform
	};

	public double Current { get; private set; }

	public bool IsPlaying { get; private set; }

	public List<BitmapSource> Thumbnails { get; } = new List<BitmapSource>();

	public float[] Waveform { get; private set; } = Array.Empty<float>();

	public bool IsOpen => _ready;

	public bool CanPlay
	{
		get
		{
			if (Player != null)
			{
				return _ready;
			}
			return false;
		}
	}

	public event Action? Changed;

	public event Action? Opened;

	public event Action? StripLoaded;

	private MediaSession(string file, MediaInfo info)
	{
		File = file;
		Info = info;
		FrameStep = 1.0 / Math.Max((info.FrameRate > 0.0) ? info.FrameRate : 30.0, 1.0);
		_timer.Tick += delegate
		{
			Tick();
		};
		_stillTimer.Tick += async delegate
		{
			_stillTimer.Stop();
			await ShowStill(Current);
		};
		Player = new MediaElement
		{
			LoadedBehavior = MediaState.Manual,
			UnloadedBehavior = MediaState.Manual,
			ScrubbingEnabled = true,
			Stretch = Stretch.Uniform,
			Volume = 1.0
		};
		Player.MediaOpened += delegate
		{
			_ready = true;
			Player.Play();
			Player.Pause();
			Player.Position = TimeSpan.Zero;
			if (Info.HasVideo)
			{
				Still.Visibility = Visibility.Collapsed;
			}
			this.Opened?.Invoke();
			this.Changed?.Invoke();
		};
		Player.MediaFailed += delegate
		{
			Drop();
		};
		Player.MediaEnded += delegate
		{
			Pause();
			this.Changed?.Invoke();
		};
		Player.Source = new Uri(file);
	}

	public static MediaSession? Load(string file)
	{
		MediaInfo result = Task.Run(() => MediaProbe.Info(file)).GetAwaiter().GetResult();
		if (result.Duration <= 0.0)
		{
			Hud.Shared.ShowMessage(L10n.T("Couldn't read the length of " + Path.GetFileName(file)), Hud.Style.Failure);
			return null;
		}
		MediaSession mediaSession = new MediaSession(file, result);
		if (result.HasVideo)
		{
			mediaSession.ShowStill(0.0);
		}
		return mediaSession;
	}

	public void Attach(Window window, int thumbnails = 10, int bars = 240)
	{
		window.Closed += delegate
		{
			Dispose();
		};
		LoadStrip(thumbnails, bars);
	}

	private void Drop()
	{
		if (Player != null)
		{
			Player.Close();
			Player = null;
			_ready = false;
			Still.Visibility = Visibility.Visible;
			this.Changed?.Invoke();
		}
	}

	private void Tick()
	{
		if (Player == null || !_ready)
		{
			return;
		}
		Current = Math.Min(Player.Position.TotalSeconds, Duration);
		double? stopAt = _stopAt;
		if (stopAt.HasValue)
		{
			double valueOrDefault = stopAt.GetValueOrDefault();
			if (Current >= valueOrDefault)
			{
				Pause();
			}
		}
		this.Changed?.Invoke();
	}

	public void Seek(double seconds)
	{
		Current = Math.Clamp(seconds, 0.0, Duration);
		if (Player != null && _ready)
		{
			Player.Position = TimeSpan.FromSeconds(Current);
		}
		else if (Info.HasVideo)
		{
			_stillTimer.Stop();
			_stillTimer.Start();
		}
		this.Changed?.Invoke();
	}

	public void Step(int frames)
	{
		Pause();
		Seek(Current + (double)frames * FrameStep);
	}

	public void Pause()
	{
		Player?.Pause();
		_timer.Stop();
		IsPlaying = false;
		_stopAt = null;
		this.Changed?.Invoke();
	}

	public void Play(double? start = null, double? end = null, double rate = 1.0)
	{
		if (Player == null || !_ready)
		{
			return;
		}
		if (IsPlaying)
		{
			Pause();
			return;
		}
		if (start.HasValue)
		{
			double valueOrDefault = start.GetValueOrDefault();
			Seek(valueOrDefault);
		}
		else if (Current >= Duration - 0.05)
		{
			Seek(0.0);
		}
		_stopAt = end;
		Player.SpeedRatio = rate;
		Player.Play();
		Still.Visibility = (Info.HasVideo ? Visibility.Collapsed : Visibility.Visible);
		IsPlaying = true;
		_timer.Start();
		this.Changed?.Invoke();
	}

	public void SetRate(double rate)
	{
		if (Player != null)
		{
			Player.SpeedRatio = rate;
		}
	}

	private async Task ShowStill(double time)
	{
		BitmapSource bitmapSource = await Frame(time, 540);
		if (bitmapSource != null)
		{
			Still.Source = bitmapSource;
		}
	}

	public async Task<BitmapSource?> Frame(double seconds, int height = 200)
	{
		return Pictures.FromBytes(await Task.Run(() => MediaTools.Thumbnail(File, Math.Min(seconds, Math.Max(0.0, Duration - 0.04)), height)));
	}

	private async Task LoadStrip(int count, int bars)
	{
		if (Info.HasVideo)
		{
			for (int index = 0; index < count; index++)
			{
				if (_disposed)
				{
					break;
				}
				BitmapSource bitmapSource = await Frame(Duration * ((double)index + 0.5) / (double)count, 90);
				if (bitmapSource != null)
				{
					Thumbnails.Add(bitmapSource);
				}
				this.StripLoaded?.Invoke();
			}
		}
		else if (Info.HasAudio)
		{
			Waveform = await Task.Run(() => MediaOps.Waveform(File, bars));
			this.StripLoaded?.Invoke();
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_timer.Stop();
			Player?.Stop();
			Player?.Close();
		}
	}
}
