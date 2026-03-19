using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StockPlacementAvaloniaView : UserControl
{
    public StockPlacementAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StockPlacementAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
