using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class EInvoiceAvaloniaView : UserControl
{
    public EInvoiceAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is EInvoiceAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
