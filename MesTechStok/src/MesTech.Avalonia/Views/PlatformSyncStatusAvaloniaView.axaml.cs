using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class PlatformSyncStatusAvaloniaView : UserControl
{
    public PlatformSyncStatusAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is PlatformSyncStatusAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
