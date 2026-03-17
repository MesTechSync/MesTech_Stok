using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CargoAvaloniaView : UserControl
{
    public CargoAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CargoAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
