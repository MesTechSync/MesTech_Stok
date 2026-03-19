using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class LabelPreviewAvaloniaView : UserControl
{
    public LabelPreviewAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is LabelPreviewAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
