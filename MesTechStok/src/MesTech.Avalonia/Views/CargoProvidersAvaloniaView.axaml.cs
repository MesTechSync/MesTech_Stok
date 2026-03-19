using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CargoProvidersAvaloniaView : UserControl
{
    public CargoProvidersAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CargoProvidersAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
