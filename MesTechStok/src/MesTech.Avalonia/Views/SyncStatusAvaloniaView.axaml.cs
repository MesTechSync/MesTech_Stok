using Avalonia;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

public partial class SyncStatusAvaloniaView : BaseView
{
    public SyncStatusAvaloniaView()
    {
        InitializeComponent();
    }

    protected override void SubscribeEvents()
    {
        base.SubscribeEvents();

        // Start 60s auto-refresh timer after view attaches
        if (DataContext is SyncStatusAvaloniaViewModel vm)
        {
            vm.StartAutoRefresh();
        }
    }
}
