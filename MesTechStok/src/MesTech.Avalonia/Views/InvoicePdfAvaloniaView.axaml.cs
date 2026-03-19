using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InvoicePdfAvaloniaView : UserControl
{
    public InvoicePdfAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InvoicePdfAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
