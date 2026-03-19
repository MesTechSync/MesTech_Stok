using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class KomisyonAvaloniaView : UserControl
{
    public KomisyonAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is KomisyonAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
