using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InvoiceReportAvaloniaView : UserControl
{
    public InvoiceReportAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InvoiceReportAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
