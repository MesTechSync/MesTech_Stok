using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DocumentsAvaloniaView : UserControl
{
    public DocumentsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DocumentsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
