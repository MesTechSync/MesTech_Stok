using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StockTransferAvaloniaView : UserControl
{
    public StockTransferAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StockTransferAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
