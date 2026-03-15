using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ReportsAvaloniaView : UserControl
{
    public ReportsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ReportsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
