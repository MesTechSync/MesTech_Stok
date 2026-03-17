using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class InventoryAvaloniaView : UserControl
{
    public InventoryAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is InventoryAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
