using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class NotificationSettingsAvaloniaView : UserControl
{
    public NotificationSettingsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is NotificationSettingsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
