using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class PrintSettingsDialog : Window
{
    public bool Result { get; private set; }
    public string? SelectedLabelSize => (LabelSizeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public int Copies => (int)(CopyCount.Value ?? 1);
    public string? SelectedPrinter => (PrinterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();

    public PrintSettingsDialog()
    {
        InitializeComponent();
        LabelSizeCombo.SelectedIndex = 0;
        PrinterCombo.SelectedIndex = 0;
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
