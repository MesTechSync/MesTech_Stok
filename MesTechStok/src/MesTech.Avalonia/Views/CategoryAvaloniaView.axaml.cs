using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CategoryAvaloniaView : UserControl
{
    public CategoryAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CategoryAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
