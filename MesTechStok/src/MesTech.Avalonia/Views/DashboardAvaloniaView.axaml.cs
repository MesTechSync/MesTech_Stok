using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DashboardAvaloniaView : UserControl
{
    public DashboardAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DashboardAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
