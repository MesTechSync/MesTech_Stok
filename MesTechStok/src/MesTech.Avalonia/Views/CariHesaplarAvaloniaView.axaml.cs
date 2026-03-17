using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CariHesaplarAvaloniaView : UserControl
{
    public CariHesaplarAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CariHesaplarAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
