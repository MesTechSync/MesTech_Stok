using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public class TreeSelectItem
{
    public string Name { get; set; } = string.Empty;
    public object? Tag { get; set; }
    public ObservableCollection<TreeSelectItem> Children { get; set; } = new();
}

public partial class TreeSelectDialog : Window
{
    public bool Result { get; private set; }
    public TreeSelectItem? SelectedItem { get; private set; }

    public TreeSelectDialog() : this(string.Empty, Enumerable.Empty<TreeSelectItem>()) { }

    public TreeSelectDialog(string title, IEnumerable<TreeSelectItem> items)
    {
        InitializeComponent();
        TitleText.Text = title;
        TreeItems.ItemsSource = items;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        SelectedItem = TreeItems.SelectedItem as TreeSelectItem;
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
