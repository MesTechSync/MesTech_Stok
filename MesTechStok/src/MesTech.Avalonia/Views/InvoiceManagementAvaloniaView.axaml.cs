using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InvoiceManagementAvaloniaView : UserControl
{
    public InvoiceManagementAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InvoiceManagementAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
