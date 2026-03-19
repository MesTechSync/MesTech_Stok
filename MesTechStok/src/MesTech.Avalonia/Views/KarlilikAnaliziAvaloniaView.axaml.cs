using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class KarlilikAnaliziAvaloniaView : UserControl
{
    public KarlilikAnaliziAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is KarlilikAnaliziAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
