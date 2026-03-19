using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class CrmSettingsAvaloniaView : UserControl
{
    public CrmSettingsAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CrmSettingsAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
