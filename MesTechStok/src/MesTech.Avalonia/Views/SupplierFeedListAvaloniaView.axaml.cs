using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class SupplierFeedListAvaloniaView : UserControl
{
    public SupplierFeedListAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SupplierFeedListAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
