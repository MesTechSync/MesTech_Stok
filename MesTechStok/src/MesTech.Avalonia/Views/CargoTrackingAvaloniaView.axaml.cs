using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CargoTrackingAvaloniaView : UserControl
{
    public CargoTrackingAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CargoTrackingAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
