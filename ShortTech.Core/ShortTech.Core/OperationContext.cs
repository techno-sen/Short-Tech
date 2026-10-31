using System;
using System.Threading;

namespace ShortTech.Core;

public sealed class OperationContext
{
	private readonly Action<string>? _report;

	private readonly Action<double>? _progress;

	public CancellationToken Cancellation { get; }

	public string? Message { get; private set; }

	public OperationContext(Action<string>? report = null, Action<double>? progress = null, CancellationToken cancellation = default(CancellationToken))
	{
		_report = report;
		_progress = progress;
		Cancellation = cancellation;
	}

	public void Report(string text)
	{
		_report?.Invoke(L10n.T(text));
	}

	public void Progress(double fraction)
	{
		_progress?.Invoke(Math.Clamp(fraction, 0.0, 1.0));
	}

	public Action<double> ProgressFor(int index, int count)
	{
		return delegate(double fraction)
		{
			Progress(((double)index + fraction) / (double)Math.Max(count, 1));
		};
	}

	public void Finish(string text)
	{
		Message = text;
	}
}
