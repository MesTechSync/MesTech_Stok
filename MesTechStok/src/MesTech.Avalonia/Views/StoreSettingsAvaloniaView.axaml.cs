using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class StoreSettingsAvaloniaView : UserControl
{
    public StoreSettingsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is StoreSettingsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
