using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public class SelectDialogItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? Tag { get; set; }
}

public partial class SelectDialog : Window
{
    private readonly List<SelectDialogItem> _allItems;
    public bool Result { get; private set; }
    public SelectDialogItem? SelectedItem { get; private set; }

    public SelectDialog(string title, IEnumerable<SelectDialogItem> items)
    {
        InitializeComponent();
        TitleText.Text = title;
        _allItems = items.ToList();
        ItemsList.ItemsSource = _allItems;

        SearchBox.TextChanged += OnSearchChanged;
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query))
        {
            ItemsList.ItemsSource = _allItems;
        }
        else
        {
            ItemsList.ItemsSource = _allItems
                .Where(i => i.Name.ToLowerInvariant().Contains(query)
                         || i.Description.ToLowerInvariant().Contains(query))
                .ToList();
        }
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        SelectedItem = ItemsList.SelectedItem as SelectDialogItem;
        if (SelectedItem == null) return;
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
