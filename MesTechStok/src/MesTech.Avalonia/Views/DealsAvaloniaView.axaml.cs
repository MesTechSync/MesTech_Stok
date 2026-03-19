using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DealsAvaloniaView : UserControl
{
    public DealsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DealsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
