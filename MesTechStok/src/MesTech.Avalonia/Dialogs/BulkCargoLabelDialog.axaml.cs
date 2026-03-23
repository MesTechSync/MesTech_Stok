using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class BulkCargoLabelDialog : Window
{
    public bool Result { get; private set; }

    public string LabelFormat
    {
        get
        {
            if (FormatPdf.IsChecked == true) return "PDF";
            if (FormatPng.IsChecked == true) return "PNG";
            return "ZPL";
        }
    }

    public string? CargoProvider => (CargoProviderCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

    public BulkCargoLabelDialog()
    {
        InitializeComponent();
    }

    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        // DataGrid select all logic — to be connected via ViewModel binding
    }

    private void OnDeselectAll(object? sender, RoutedEventArgs e)
    {
        // DataGrid deselect all logic
    }

    private void OnPrint(object? sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
