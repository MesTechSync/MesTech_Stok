using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CategoryMappingAvaloniaView : UserControl
{
    public CategoryMappingAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CategoryMappingAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
