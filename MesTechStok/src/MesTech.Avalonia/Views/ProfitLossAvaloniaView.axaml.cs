using Avalonia.Controls;
using MesTechStok.Desktop.ViewModels.Finance;

namespace MesTech.Avalonia.Views;

public partial class ProfitLossAvaloniaView : UserControl
{
    public ProfitLossAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            // ProfitLossViewModel is compile-linked from WPF Desktop — ZERO CHANGES
            if (DataContext is ProfitLossViewModel vm)
                await vm.LoadAsync();
        };
    }
}
