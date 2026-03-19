using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class NakitAkisAvaloniaView : UserControl
{
    public NakitAkisAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is NakitAkisAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
