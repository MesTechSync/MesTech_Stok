using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ActivityAvaloniaView : UserControl
{
    public ActivityAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ActivityAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
