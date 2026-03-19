using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class KanbanBoardAvaloniaView : UserControl
{
    public KanbanBoardAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is KanbanBoardAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
