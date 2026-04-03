using Avalonia.Controls;
using Avalonia.Input;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

public partial class BarcodeScannerView : BaseView
{
    public BarcodeScannerView()
    {
        InitializeComponent();
    }

    protected override void SubscribeEvents()
    {
        base.SubscribeEvents();
        // Auto-focus barcode input for USB HID keyboard wedge
        var scanInput = this.FindControl<TextBox>("ScanInputBox");
        scanInput?.Focus();
    }
}
