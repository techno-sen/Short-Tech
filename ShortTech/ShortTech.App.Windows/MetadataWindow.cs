using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ShortTech.Core;
using ShortTech.Core.Images;

namespace ShortTech.App.Windows;

public static class MetadataWindow
{
	public static Window Make(IReadOnlyList<string> files)
	{
		string file = files[0];
		List<MetadataField> fields = ShortTech.Core.Images.ImageMetadata.Read(file);
		ToolWindow window = new ToolWindow("Metadata", 560.0);
		StackPanel list = new StackPanel();
		TextBox search = new TextBox
		{
			Height = 26.0,
			MinWidth = 200.0,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = false,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		string? fileName = Path.GetFileName(file);
		Brush muted = Theme.Muted;
		TextBlock element = UI.Text(fileName, 12.5, null, muted);
		DockPanel.SetDock(element, Dock.Left);
		DockPanel.SetDock(search, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(search);
		search.TextChanged += delegate
		{
			Draw();
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(dockPanel);
		stackPanel.Children.Add(new ScrollViewer
		{
			Content = list,
			MaxHeight = 420.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		});
		stackPanel.Children.Add(UI.Caption("For photos, Remove All also clears hidden camera and processing data. The original stays unchanged.").Pad(0.0, 10.0));
		Draw();
		window.Body = stackPanel;
		window.Leading(UI.Button("Remove All", delegate
		{
			window.Close();
			Runner.Run("Remove Metadata", new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await ShortTech.Core.Images.ImageMetadata.Save(f[0], fields, removeAll: true, c)));
		}));
		window.Trailing(UI.Button("Save", delegate
		{
			if (!fields.Any((MetadataField f) => f.Changed))
			{
				window.Close();
			}
			else
			{
				window.Close();
				Runner.Run("Edit Metadata", new global::_003C_003Ez__ReadOnlySingleElementList<string>(file), async (IReadOnlyList<string> f, OperationContext c) => new global::_003C_003Ez__ReadOnlySingleElementList<string>(await ShortTech.Core.Images.ImageMetadata.Save(f[0], fields, removeAll: false, c)));
			}
		}, primary: true));
		return window;
		void Draw()
		{
			list.Children.Clear();
			string query = search.Text.Trim();
			List<MetadataField> list2 = fields.Where((MetadataField f) => !f.Removed && (query.Length == 0 || f.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || f.Value.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
			if (list2.Count == 0)
			{
				list.Children.Add(UI.Caption("No matching metadata."));
			}
			foreach (IGrouping<string, MetadataField> item in from f in list2
				group f by f.Group)
			{
				list.Children.Add(UI.Text(item.Key, 11.5, FontWeights.SemiBold, Theme.Muted).Pad(0.0, 6.0));
				foreach (MetadataField field in item)
				{
					Grid grid = new Grid
					{
						Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
					};
					grid.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = new GridLength(170.0)
					});
					grid.ColumnDefinitions.Add(new ColumnDefinition());
					grid.ColumnDefinitions.Add(new ColumnDefinition
					{
						Width = GridLength.Auto
					});
					TextBlock textBlock = UI.Text(L10n.T(field.Name) + ":", 12.5);
					textBlock.TextAlignment = TextAlignment.Right;
					textBlock.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
					grid.Children.Add(textBlock);
					FrameworkElement element2;
					if (field.Editable)
					{
						TextBox box = new TextBox
						{
							Text = field.Value,
							Height = 26.0,
							VerticalContentAlignment = VerticalAlignment.Center
						};
						box.TextChanged += delegate
						{
							field.Value = box.Text;
						};
						element2 = box;
					}
					else
					{
						element2 = UI.Text(field.Value, 12.5);
					}
					Grid.SetColumn(element2, 1);
					grid.Children.Add(element2);
					if (field.Group != "File")
					{
						Button button = UI.IconButton("\ue738", delegate
						{
							field.Removed = true;
							Draw();
						}, "Remove this field", 22.0);
						Grid.SetColumn(button, 2);
						button.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
						grid.Children.Add(button);
					}
					list.Children.Add(grid);
				}
			}
		}
	}
}
