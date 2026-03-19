using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DocumentManagerAvaloniaView : UserControl
{
    public DocumentManagerAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DocumentManagerAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
