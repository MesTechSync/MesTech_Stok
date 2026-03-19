using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InvoiceProviderSettingsAvaloniaView : UserControl
{
    public InvoiceProviderSettingsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InvoiceProviderSettingsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
