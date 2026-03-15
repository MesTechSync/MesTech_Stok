using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class LeadsAvaloniaView : UserControl
{
    public LeadsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is LeadsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
