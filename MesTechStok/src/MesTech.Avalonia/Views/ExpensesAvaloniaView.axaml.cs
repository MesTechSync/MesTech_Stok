using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ExpensesAvaloniaView : UserControl
{
    public ExpensesAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ExpensesAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
