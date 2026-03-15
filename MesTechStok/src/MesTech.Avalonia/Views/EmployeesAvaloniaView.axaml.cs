using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class EmployeesAvaloniaView : UserControl
{
    public EmployeesAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is EmployeesAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
