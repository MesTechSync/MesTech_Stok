using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class AuditLogAvaloniaView : UserControl
{
    public AuditLogAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is AuditLogAvaloniaViewModel vm)
                await vm.LoadAsync();
        };
    }
}
