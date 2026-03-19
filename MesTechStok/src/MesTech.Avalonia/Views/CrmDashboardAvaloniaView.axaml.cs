using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CrmDashboardAvaloniaView : UserControl
{
    public CrmDashboardAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CrmDashboardAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
