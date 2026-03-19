using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BulkShipmentAvaloniaView : UserControl
{
    public BulkShipmentAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BulkShipmentAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
