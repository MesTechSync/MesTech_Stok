using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ProductsAvaloniaView : UserControl
{
    public ProductsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ProductsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
