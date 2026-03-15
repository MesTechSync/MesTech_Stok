using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class SettingsAvaloniaView : UserControl
{
    public SettingsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SettingsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
