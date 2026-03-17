using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CariAvaloniaView : UserControl
{
    public CariAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CariAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
