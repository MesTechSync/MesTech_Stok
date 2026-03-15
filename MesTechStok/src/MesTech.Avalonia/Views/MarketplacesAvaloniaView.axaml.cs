using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class MarketplacesAvaloniaView : UserControl
{
    public MarketplacesAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is MarketplacesAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
