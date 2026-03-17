using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StockUpdateAvaloniaView : UserControl
{
    public StockUpdateAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StockUpdateAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
