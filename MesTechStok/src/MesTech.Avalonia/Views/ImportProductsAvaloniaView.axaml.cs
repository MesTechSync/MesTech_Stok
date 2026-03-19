using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ImportProductsAvaloniaView : UserControl
{
    public ImportProductsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ImportProductsAvaloniaViewModel vm)
                await vm.InitializeAsync();
        };
    }
}
