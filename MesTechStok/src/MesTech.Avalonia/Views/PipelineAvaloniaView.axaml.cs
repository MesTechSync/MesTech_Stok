using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class PipelineAvaloniaView : UserControl
{
    public PipelineAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is PipelineAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
