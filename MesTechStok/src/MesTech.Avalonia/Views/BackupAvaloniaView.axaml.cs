using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BackupAvaloniaView : UserControl
{
    public BackupAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BackupAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
