using Avalonia.Controls;
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
}
