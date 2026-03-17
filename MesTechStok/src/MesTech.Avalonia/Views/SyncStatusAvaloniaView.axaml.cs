using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class SyncStatusAvaloniaView : UserControl
{
    public SyncStatusAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SyncStatusAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
