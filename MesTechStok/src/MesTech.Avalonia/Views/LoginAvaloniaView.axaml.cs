using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class LoginAvaloniaView : UserControl
{
    public LoginAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is LoginAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
