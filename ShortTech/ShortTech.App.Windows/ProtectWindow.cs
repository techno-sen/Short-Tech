using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Pdf;

namespace ShortTech.App.Windows;

public static class ProtectWindow
{
	private static readonly Brush Problem = new SolidColorBrush(Color.FromRgb(196, 43, 28));

	public static Window Make(IReadOnlyList<string> files)
	{
		ToolWindow window = new ToolWindow("Protect with Password", 440.0);
		StackPanel stack = new StackPanel();
		PasswordBox first = new PasswordBox
		{
			Width = 200.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		PasswordBox second = new PasswordBox
		{
			Width = 200.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		stack.Children.Add(UI.Row("Password", first));
		Grid confirmRow = UI.Row("Confirm password", second);
		confirmRow.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		stack.Children.Add(confirmRow);
		stack.Children.Add(UI.Caption("Anyone opening the PDF will need this password. It cannot be recovered if you forget it.").Pad(0.0, 8.0));
		TextBlock message = UI.Text("", 12.0, null, Problem, wrap: true);
		stack.Children.Add(message);
		window.Body = stack;
		Button protect = UI.Button("Protect", delegate
		{
			string password = first.Password;
			if (password.Length > 0 && password == second.Password)
			{
				window.Close();
				Runner.Run("Protect with Password", files, (IReadOnlyList<string> f, OperationContext c) => PdfOps.Protect(f, password, c));
			}
		}, primary: true);
		protect.IsDefault = true;
		protect.IsEnabled = false;
		first.PasswordChanged += delegate
		{
			Refresh();
		};
		second.PasswordChanged += delegate
		{
			Refresh();
		};
		window.Trailing(protect);
		window.Loaded += delegate
		{
			first.Focus();
		};
		return window;
		void Refresh()
		{
			bool same = first.Password == second.Password;
			protect.IsEnabled = first.Password.Length > 0 && same;
			message.Text = (second.Password.Length > 0 && !same) ? L10n.T("The passwords do not match.") : "";
		}
	}
}
