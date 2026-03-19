using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public class ColumnSelectItem
{
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = true;
}

public partial class ColumnSelectDialog : Window
{
    private readonly List<CheckBox> _checkBoxes = new();
    public bool Result { get; private set; }
    public List<string> SelectedColumns { get; private set; } = new();

    public ColumnSelectDialog(IEnumerable<ColumnSelectItem> columns)
    {
        InitializeComponent();

        foreach (var col in columns)
        {
            var cb = new CheckBox
            {
                Content = col.Name,
                IsChecked = col.IsSelected,
                Tag = col.Name
            };
            _checkBoxes.Add(cb);
            ColumnsPanel.Children.Add(cb);
        }
    }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        SelectedColumns = _checkBoxes
            .Where(cb => cb.IsChecked == true)
            .Select(cb => cb.Tag?.ToString() ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
