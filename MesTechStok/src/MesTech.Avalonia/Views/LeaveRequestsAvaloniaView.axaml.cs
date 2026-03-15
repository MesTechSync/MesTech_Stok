using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class LeaveRequestsAvaloniaView : UserControl
{
    public LeaveRequestsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is LeaveRequestsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
