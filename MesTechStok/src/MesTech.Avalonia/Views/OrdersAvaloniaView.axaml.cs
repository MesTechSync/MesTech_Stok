using Avalonia.Input;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

public partial class OrdersAvaloniaView : BaseView
{
    public OrdersAvaloniaView()
    {
        InitializeComponent();
    }

    // HH-DEV2-010: Navigate to order detail on double-click
    private void OnOrderDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is OrdersAvaloniaViewModel vm && vm.SelectedOrder is not null)
        {
            vm.ShowOrderDetailCommand.Execute(null);
        }
    }
}
