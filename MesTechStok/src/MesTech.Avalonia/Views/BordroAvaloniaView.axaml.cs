using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BordroAvaloniaView : UserControl
{
    public BordroAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BordroAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
