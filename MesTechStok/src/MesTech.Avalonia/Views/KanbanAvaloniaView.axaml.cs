using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class KanbanAvaloniaView : UserControl
{
    public KanbanAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is KanbanAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
