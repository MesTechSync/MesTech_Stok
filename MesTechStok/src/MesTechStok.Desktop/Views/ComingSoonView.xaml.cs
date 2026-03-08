using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class ComingSoonView : UserControl
    {
        public ComingSoonView(string moduleName = "")
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(moduleName))
                ModuleNameText.Text = $"Modul: {moduleName}";
            else
                ModuleNameText.Text = "Bu modul gelistirme asamasindadir.";
        }
    }
}
