using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ProductVariantMatrixView : UserControl
{
    public ProductVariantMatrixView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ProductVariantMatrixViewModel vm)
                await vm.LoadAsync();
        };
    }
}
