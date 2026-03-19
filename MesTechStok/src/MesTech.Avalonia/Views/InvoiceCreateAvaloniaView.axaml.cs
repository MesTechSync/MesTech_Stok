using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InvoiceCreateAvaloniaView : UserControl
{
    public InvoiceCreateAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InvoiceCreateAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
