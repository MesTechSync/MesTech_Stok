using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StoreDetailAvaloniaView : UserControl
{
    public StoreDetailAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StoreDetailAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
