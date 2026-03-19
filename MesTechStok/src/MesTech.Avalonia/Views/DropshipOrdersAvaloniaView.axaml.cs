using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DropshipOrdersAvaloniaView : UserControl
{
    public DropshipOrdersAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DropshipOrdersAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
