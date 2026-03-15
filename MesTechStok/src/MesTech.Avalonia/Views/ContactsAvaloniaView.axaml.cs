using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ContactsAvaloniaView : UserControl
{
    public ContactsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ContactsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
