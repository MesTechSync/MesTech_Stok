using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class WarehouseAvaloniaView : UserControl
{
    public WarehouseAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is WarehouseAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
