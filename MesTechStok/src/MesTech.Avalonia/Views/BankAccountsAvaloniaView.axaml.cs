using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BankAccountsAvaloniaView : UserControl
{
    public BankAccountsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BankAccountsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
