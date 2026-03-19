using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ProjectsAvaloniaView : UserControl
{
    public ProjectsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ProjectsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
