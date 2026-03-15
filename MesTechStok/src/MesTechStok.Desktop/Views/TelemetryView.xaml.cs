using System.Windows.Controls;
using MesTechStok.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MesTechStok.Desktop.Views;

public partial class TelemetryView : UserControl
{
    public TelemetryView()
    {
        InitializeComponent();
        if (Desktop.App.Services != null)
        {
            var vm = Desktop.App.Services.GetRequiredService<TelemetryViewModel>();
            DataContext = vm;
            _ = vm.InitializeAsync();
        }
    }
}
