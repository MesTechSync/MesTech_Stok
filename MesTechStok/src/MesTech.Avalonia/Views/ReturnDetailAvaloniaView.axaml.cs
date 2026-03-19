using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ReturnDetailAvaloniaView : UserControl
{
    public ReturnDetailAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ReturnDetailAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
