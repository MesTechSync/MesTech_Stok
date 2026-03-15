using System.Windows.Controls;
using System.Windows.Input;

namespace MesTechStok.Desktop.Views.EInvoice
{
    public partial class EInvoiceCreateView : UserControl
    {
        public EInvoiceCreateView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Allow only numeric input for the VKN/TCKN field (10-11 digits).
        /// </summary>
        private void Vkn_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text);
        }

        private static bool IsNumeric(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }
    }
}
