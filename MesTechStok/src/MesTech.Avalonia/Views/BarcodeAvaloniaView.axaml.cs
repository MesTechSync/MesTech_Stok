using Avalonia.Controls;
using Avalonia.Input;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BarcodeAvaloniaView : UserControl
{
    public BarcodeAvaloniaView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // Auto-focus barcode input for USB HID keyboard wedge
            var barcodeInput = this.FindControl<TextBox>("BarcodeInput");
            barcodeInput?.Focus();
        };
    }
}
