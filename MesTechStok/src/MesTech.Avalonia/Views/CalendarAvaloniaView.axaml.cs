using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CalendarAvaloniaView : UserControl
{
    public CalendarAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CalendarAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
