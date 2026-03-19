using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class VergiTakvimiAvaloniaView : UserControl
{
    public VergiTakvimiAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is VergiTakvimiAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
