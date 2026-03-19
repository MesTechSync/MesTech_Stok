using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DropshipDashboardAvaloniaView : UserControl
{
    public DropshipDashboardAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DropshipDashboardAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
