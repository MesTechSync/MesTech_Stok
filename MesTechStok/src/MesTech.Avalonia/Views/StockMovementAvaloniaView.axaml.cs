using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StockMovementAvaloniaView : UserControl
{
    public StockMovementAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StockMovementAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
