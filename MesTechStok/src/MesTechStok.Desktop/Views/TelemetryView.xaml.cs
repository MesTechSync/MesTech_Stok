using System.Windows.Controls;
using MesTechStok.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MesTechStok.Desktop.Views;

public partial class TelemetryView : UserControl
{
    public TelemetryView()
    {
        InitializeComponent();
        if (Desktop.App.ServiceProvider != null)
        {
            var vm = Desktop.App.ServiceProvider.GetRequiredService<TelemetryViewModel>();
            DataContext = vm;
            _ = vm.InitializeAsync();
        }
    }
}
