using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DepartmentAvaloniaView : UserControl
{
    public DepartmentAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DepartmentAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
