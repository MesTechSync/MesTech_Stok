using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class KdvRaporAvaloniaView : UserControl
{
    public KdvRaporAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is KdvRaporAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
