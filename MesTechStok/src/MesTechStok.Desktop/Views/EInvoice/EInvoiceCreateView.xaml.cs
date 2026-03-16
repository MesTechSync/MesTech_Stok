using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MesTechStok.Desktop.Views.EInvoice
{
    public partial class EInvoiceCreateView : UserControl
    {
        public EInvoiceCreateView()
        {
            InitializeComponent();
            Loaded += EInvoiceCreateView_Loaded;
        }

        private void EInvoiceCreateView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowContent();
                // Attach validation on VKN field lost focus
                TxtVkn.LostFocus += TxtVkn_LostFocus;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[EInvoiceCreateView] Loaded error: {ex.Message}");
                ShowError($"Form yuklenirken hata: {ex.Message}");
            }
        }

        /// <summary>
        /// Allow only numeric input for the VKN/TCKN field (10-11 digits).
        /// </summary>
        private void Vkn_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text);
        }

        /// <summary>
        /// Validate VKN/TCKN length when the field loses focus.
        /// VKN = 10 digits, TCKN = 11 digits.
        /// </summary>
        private void TxtVkn_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var vkn = TxtVkn.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(vkn)) return;

                if (vkn.Length != 10 && vkn.Length != 11)
                {
                    TxtVkn.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xDC, 0x35, 0x45));
                    TxtVkn.ToolTip = "VKN 10 haneli, TCKN 11 haneli olmalidir";
                }
                else
                {
                    TxtVkn.ClearValue(Border.BorderBrushProperty);
                    TxtVkn.ToolTip = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[EInvoiceCreateView] VKN validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates the entire form before submission.
        /// Returns true if all required fields are filled and valid.
        /// </summary>
        public bool ValidateForm()
        {
            var errors = new System.Collections.Generic.List<string>();

            // VKN/TCKN validation
            var vkn = TxtVkn.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(vkn))
            {
                errors.Add("VKN/TCKN alani zorunludur");
            }
            else if (vkn.Length != 10 && vkn.Length != 11)
            {
                errors.Add("VKN 10 haneli, TCKN 11 haneli olmalidir");
            }

            // Date validation
            if (DpDuzenleme.SelectedDate is null)
            {
                errors.Add("Duzenleme tarihi zorunludur");
            }
            else if (DpDuzenleme.SelectedDate > DateTime.Today)
            {
                errors.Add("Duzenleme tarihi gelecek bir tarih olamaz");
            }

            // Invoice lines validation
            if (InvoiceLinesGrid.Items.Count == 0)
            {
                errors.Add("En az bir fatura satiri eklenmeli");
            }

            if (errors.Count > 0)
            {
                ShowError(string.Join("\n", errors));
                return false;
            }

            return true;
        }

        private static bool IsNumeric(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }

        #region Loading/Empty/Error State Helpers
        private void ShowLoading()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void ShowEmpty()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string msg = "Bir hata olustu")
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Visible;
            ErrorMessage.Text = msg;
        }

        private void ShowContent()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowContent();
        }
        #endregion
    }
}
