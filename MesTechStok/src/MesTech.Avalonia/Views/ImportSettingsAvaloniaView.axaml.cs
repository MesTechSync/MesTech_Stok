using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class ImportSettingsAvaloniaView : UserControl
{
    public ImportSettingsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is ImportSettingsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
