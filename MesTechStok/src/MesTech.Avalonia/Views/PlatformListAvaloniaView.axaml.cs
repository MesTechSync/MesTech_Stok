using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class PlatformListAvaloniaView : UserControl
{
    public PlatformListAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is PlatformListAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
