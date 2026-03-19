using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class SabitGiderlerAvaloniaView : UserControl
{
    public SabitGiderlerAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is SabitGiderlerAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
