using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class PlatformMessagesAvaloniaView : UserControl
{
    public PlatformMessagesAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is PlatformMessagesAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
