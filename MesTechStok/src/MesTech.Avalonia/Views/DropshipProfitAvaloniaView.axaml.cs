using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DropshipProfitAvaloniaView : UserControl
{
    public DropshipProfitAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DropshipProfitAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
