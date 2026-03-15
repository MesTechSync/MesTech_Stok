using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.ViewModels.Documents;

namespace MesTechStok.Desktop.Views.Documents;

public partial class DocumentManagerView : UserControl
{
    public DocumentManagerView()
    {
        InitializeComponent();
        if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        {
            DataContext = App.ServiceProvider!.GetRequiredService<DocumentManagerViewModel>();
            Loaded += async (_, _) => await ((DocumentManagerViewModel)DataContext).LoadAsync();
        }
    }
}
