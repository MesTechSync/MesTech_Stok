using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class DocumentFolderAvaloniaView : UserControl
{
    public DocumentFolderAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is DocumentFolderAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
