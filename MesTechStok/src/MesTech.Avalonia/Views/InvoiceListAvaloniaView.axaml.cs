using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InvoiceListAvaloniaView : UserControl
{
    public InvoiceListAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InvoiceListAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
