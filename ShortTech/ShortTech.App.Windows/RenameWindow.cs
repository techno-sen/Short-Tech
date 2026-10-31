using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;

namespace ShortTech.App.Windows;

public static class RenameWindow
{
	private const int PreviewLines = 5;

	private static readonly Brush Problem = new SolidColorBrush(Color.FromRgb(196, 43, 28));

	public static Window Make(IReadOnlyList<string> files)
	{
		IReadOnlyList<string> ordered = FileActions.NameOrder(files);
		bool single = ordered.Count == 1;
		string title = single ? "Rename" : "Rename Files";
		ToolWindow window = new ToolWindow(title, 480.0);
		StackPanel stack = new StackPanel();
		TextBox nameBox = new TextBox
		{
			Text = SuggestedName(ordered),
			Width = 240.0,
			Height = 26.0,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		TextBox startBox = new TextBox
		{
			Text = "1",
			Width = 60.0,
			Height = 26.0,
			TextAlignment = TextAlignment.Right,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		stack.Children.Add(UI.Row("New name", nameBox));
		Grid startRow = UI.Row("Start at", startBox);
		startRow.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		startRow.Visibility = single ? Visibility.Collapsed : Visibility.Visible;
		stack.Children.Add(startRow);
		stack.Children.Add(UI.Divider());
		stack.Children.Add(UI.Text("Preview", 12.0, FontWeights.SemiBold));
		StackPanel preview = new StackPanel
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		stack.Children.Add(preview);
		if (!single)
		{
			stack.Children.Add(UI.Caption("Files are numbered in the order Explorer shows them by name.").Pad(0.0, 6.0));
		}
		TextBlock message = UI.Text("", 12.0, null, Problem, wrap: true);
		stack.Children.Add(message);
		window.Body = stack;
		Button rename = null!;
		rename = UI.Button(title, delegate
		{
			if (Read(out var name, out var start))
			{
				window.Close();
				Runner.Run(title, ordered, (IReadOnlyList<string> f, OperationContext c) => FileActions.Rename(f, name, start, c));
			}
		}, primary: true);
		rename.IsDefault = true;
		window.Trailing(rename);
		nameBox.TextChanged += delegate
		{
			Refresh();
		};
		startBox.TextChanged += delegate
		{
			Refresh();
		};
		window.Loaded += delegate
		{
			nameBox.Focus();
			nameBox.SelectAll();
		};
		Refresh();
		return window;
		bool Read(out string name, out int start)
		{
			name = nameBox.Text.Trim();
			start = 1;
			string? problem = FileActions.RenameProblem(name);
			if (problem == null && !single && (!int.TryParse(startBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out start) || start < 0 || start > 999999))
			{
				problem = "Enter a whole number to start from.";
			}
			message.Text = (problem == null) ? "" : L10n.T(problem);
			rename.IsEnabled = problem == null;
			return problem == null;
		}
		void Refresh()
		{
			preview.Children.Clear();
			if (!Read(out var name, out var start))
			{
				return;
			}
			IReadOnlyList<string> targets = FileActions.RenameTargets(ordered, name, start);
			string arrow = L10n.IsRightToLeft ? "  ‏←‏  " : "  ‎→‎  ";
			for (int i = 0; i < ordered.Count && i < PreviewLines; i++)
			{
				TextBlock line = new TextBlock
				{
					FontFamily = Theme.Font,
					FontSize = 12.0,
					TextTrimming = TextTrimming.CharacterEllipsis,
					Margin = new Thickness(0.0, 0.0, 0.0, 3.0)
				};
				line.Inlines.Add(new System.Windows.Documents.Run(Path.GetFileName(ordered[i]))
				{
					Foreground = Theme.Muted
				});
				line.Inlines.Add(new System.Windows.Documents.Run(arrow)
				{
					Foreground = Theme.Muted
				});
				line.Inlines.Add(new System.Windows.Documents.Run(Path.GetFileName(targets[i]))
				{
					Foreground = Theme.Ink
				});
				preview.Children.Add(line);
			}
			if (ordered.Count > PreviewLines)
			{
				preview.Children.Add(UI.Caption(L10n.T($"…and {ordered.Count - PreviewLines} more")));
			}
		}
	}

	private static string SuggestedName(IReadOnlyList<string> files)
	{
		string first = files[0];
		string name = Directory.Exists(first) ? Path.GetFileName(first) : Path.GetFileNameWithoutExtension(first);
		if (files.Count == 1)
		{
			return name;
		}
		string trimmed = name.TrimEnd(" _-.()[]0123456789".ToCharArray());
		return (trimmed.Length > 0) ? trimmed : L10n.T("File");
	}
}
