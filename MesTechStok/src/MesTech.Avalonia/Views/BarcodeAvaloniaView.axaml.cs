using Avalonia.Controls;
using Avalonia.Input;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

public partial class BarcodeAvaloniaView : BaseView
{
    public BarcodeAvaloniaView()
    {
        InitializeComponent();
    }

    protected override void SubscribeEvents()
    {
        base.SubscribeEvents();
        // Auto-focus barcode input for USB HID keyboard wedge
        var barcodeInput = this.FindControl<TextBox>("BarcodeInput");
        barcodeInput?.Focus();
    }
}
