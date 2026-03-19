using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StockLotAvaloniaView : UserControl
{
    public StockLotAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StockLotAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
