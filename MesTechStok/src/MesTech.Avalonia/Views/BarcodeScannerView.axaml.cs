using Avalonia.Controls;
using Avalonia.Input;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BarcodeScannerView : UserControl
{
    public BarcodeScannerView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // Auto-focus barcode input for USB HID keyboard wedge
            var scanInput = this.FindControl<TextBox>("ScanInputBox");
            scanInput?.Focus();
        };
    }
}
