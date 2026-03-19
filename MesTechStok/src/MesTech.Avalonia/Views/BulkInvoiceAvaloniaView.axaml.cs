using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BulkInvoiceAvaloniaView : UserControl
{
    public BulkInvoiceAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BulkInvoiceAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
