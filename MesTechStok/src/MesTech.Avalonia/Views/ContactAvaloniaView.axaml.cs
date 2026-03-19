using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ContactAvaloniaView : UserControl
{
    public ContactAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ContactAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
