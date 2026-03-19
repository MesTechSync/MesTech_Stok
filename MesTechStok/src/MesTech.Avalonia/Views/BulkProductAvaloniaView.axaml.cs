using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BulkProductAvaloniaView : UserControl
{
    public BulkProductAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BulkProductAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
