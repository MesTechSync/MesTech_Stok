using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ReturnListAvaloniaView : UserControl
{
    public ReturnListAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ReturnListAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
