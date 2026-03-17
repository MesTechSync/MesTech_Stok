using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class PlatformSyncAvaloniaView : UserControl
{
    public PlatformSyncAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is PlatformSyncAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
