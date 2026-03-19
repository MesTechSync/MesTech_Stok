using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class SearchSelectDialog : Window
{
    private readonly Func<string, Task<IEnumerable<SelectDialogItem>>>? _searchFunc;
    public bool Result { get; private set; }
    public SelectDialogItem? SelectedItem { get; private set; }

    public SearchSelectDialog(string title, Func<string, Task<IEnumerable<SelectDialogItem>>>? searchFunc = null)
    {
        InitializeComponent();
        TitleText.Text = title;
        _searchFunc = searchFunc;
    }

    private async void OnSearch(object? sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        if (_searchFunc != null)
        {
            StatusText.Text = "Araniyor...";
            try
            {
                var results = await _searchFunc(query);
                ResultsList.ItemsSource = results;
                StatusText.Text = "Sonuclar listelendi.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Hata: {ex.Message}";
            }
        }
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        SelectedItem = ResultsList.SelectedItem as SelectDialogItem;
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
