using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ReportDashboardAvaloniaView : UserControl
{
    public ReportDashboardAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ReportDashboardAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
