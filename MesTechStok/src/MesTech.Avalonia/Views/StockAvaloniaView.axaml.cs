using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StockAvaloniaView : UserControl
{
    public StockAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StockAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
