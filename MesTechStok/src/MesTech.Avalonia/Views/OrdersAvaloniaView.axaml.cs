using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class OrdersAvaloniaView : UserControl
{
    public OrdersAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is OrdersAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
