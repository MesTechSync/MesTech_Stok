using Avalonia.Controls;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

/// <summary>
/// WPF005: BarcodeReaderView code-behind — auto-focuses barcode input on load.
/// </summary>
public partial class BarcodeReaderView : BaseView
{
    public BarcodeReaderView()
    {
        InitializeComponent();
    }

    protected override void SubscribeEvents()
    {
        // Auto-focus barcode input for USB HID keyboard wedge
        var inputBox = this.FindControl<TextBox>("BarcodeInputBox");
        inputBox?.Focus();
    }
}
