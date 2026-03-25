using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class BarcodeDialog : Window
{
    public string? BarcodeValue => BarcodeInput.Text;

    public BarcodeDialog() : this(null) { }

    public BarcodeDialog(string? defaultBarcode = null)
    {
        InitializeComponent();
        if (defaultBarcode != null)
        {
            BarcodeInput.Text = defaultBarcode;
            BarcodePreview.Text = defaultBarcode;
        }
    }

    private void OnGenerate(object? sender, RoutedEventArgs e)
    {
        var code = BarcodeInput.Text?.Trim();
        if (!string.IsNullOrEmpty(code))
        {
            BarcodePreview.Text = code;
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
