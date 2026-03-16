using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

#pragma warning disable CS0618 // Core.Data.Models type references — will migrate to Domain entities in H32

namespace MesTechStok.Desktop.Views
{
    public partial class CustomerEditPopup : Window
    {
        private readonly MesTechStok.Core.Services.Abstract.ICustomerService _customerService;
        private readonly ILogger<CustomerEditPopup>? _logger;
        private bool _isSaving = false;
        private Guid? _editingCustomerId;

        public CustomerEditPopup()
        {
            InitializeComponent();
            var sp = MesTechStok.Desktop.App.Services;
            _customerService = sp!.GetRequiredService<MesTechStok.Core.Services.Abstract.ICustomerService>();
            _logger = sp!.GetService<ILogger<CustomerEditPopup>>();
        }

        public CustomerEditPopup(MesTechStok.Core.Data.Models.Customer existing) : this()
        {
            _editingCustomerId = existing.Id;
            Title = "Müşteri Düzenle";
            TxtName.Text = existing.Name;
            TxtCode.Text = existing.Code;
            TxtEmail.Text = existing.Email ?? string.Empty;
            TxtPhone.Text = existing.Phone ?? string.Empty;
            (CmbType.Items[existing.CustomerType == "CORPORATE" ? 1 : existing.CustomerType == "VIP" ? 2 : 0] as System.Windows.Controls.ComboBoxItem)!.IsSelected = true;
            ChkVip.IsChecked = existing.IsVip;
            TxtBilling.Text = existing.BillingAddress ?? string.Empty;
            TxtShipping.Text = existing.ShippingAddress ?? string.Empty;
            TxtCity.Text = existing.City ?? string.Empty;
            TxtState.Text = existing.State ?? string.Empty;
            TxtPostalCode.Text = existing.PostalCode ?? string.Empty;
            TxtCountry.Text = existing.Country ?? "Türkiye";
            if (existing.CreditLimit.HasValue) TxtCreditLimit.Text = existing.CreditLimit.Value.ToString(CultureInfo.CurrentCulture);
            if (existing.DiscountRate.HasValue) TxtDiscount.Text = existing.DiscountRate.Value.ToString(CultureInfo.CurrentCulture);
            if (existing.PaymentTermDays > 0) TxtPaymentTerm.Text = existing.PaymentTermDays.ToString();
            TxtCurrency.Text = existing.Currency ?? "TRY";

            // Belgeler listesi
            try
            {
                var docs = ParseDocs(existing.DocumentUrls);
                foreach (var d in docs) DocsList.Items.Add(d);
            }
            catch (Exception ex)
            {
                // Intentional: document list parse and UI load — non-critical initial population.
                _logger?.LogWarning(ex, "{ViewName} - {Context}: {Message}", nameof(CustomerEditPopup), "Document list parse and UI load — non-critical initial population", ex.Message);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Escape) Close();
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (e.Key == Key.S || e.Key == Key.Enter))
                {
                    e.Handled = true;
                    SaveAndClose_Click(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "{View} KeyDown handler error", nameof(CustomerEditPopup));
            }
        }

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;
            var btn = sender as System.Windows.Controls.Button;
            var originalContent = btn?.Content;
            if (btn != null) { btn.IsEnabled = false; btn.Content = "Kaydediliyor..."; }

            try
            {
                var name = (TxtName.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name)) { MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Ad gerekli", "Müşteri"); return; }

                var c = new MesTechStok.Core.Data.Models.Customer
                {
                    Id = _editingCustomerId ?? Guid.Empty,
                    Name = name,
                    Code = (TxtCode.Text ?? string.Empty).Trim(),
                    Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim(),
                    CustomerType = ((System.Windows.Controls.ComboBoxItem)CmbType.SelectedItem!).Content?.ToString() ?? "INDIVIDUAL",
                    IsVip = ChkVip.IsChecked == true,
                    BillingAddress = string.IsNullOrWhiteSpace(TxtBilling.Text) ? null : TxtBilling.Text.Trim(),
                    ShippingAddress = string.IsNullOrWhiteSpace(TxtShipping.Text) ? null : TxtShipping.Text.Trim(),
                    City = string.IsNullOrWhiteSpace(TxtCity.Text) ? null : TxtCity.Text.Trim(),
                    State = string.IsNullOrWhiteSpace(TxtState.Text) ? null : TxtState.Text.Trim(),
                    PostalCode = string.IsNullOrWhiteSpace(TxtPostalCode.Text) ? null : TxtPostalCode.Text.Trim(),
                    Country = string.IsNullOrWhiteSpace(TxtCountry.Text) ? "Türkiye" : TxtCountry.Text.Trim(),
                    CreditLimit = decimal.TryParse(TxtCreditLimit.Text, out var cl) ? cl : null,
                    DiscountRate = decimal.TryParse(TxtDiscount.Text, out var dr) ? dr : null,
                    PaymentTermDays = int.TryParse(TxtPaymentTerm.Text, out var pd) ? pd : 0,
                    Currency = string.IsNullOrWhiteSpace(TxtCurrency.Text) ? "TRY" : TxtCurrency.Text.Trim(),
                    IsActive = true
                };

                // Belgeleri müşteri klasörüne kopyalayalım ve JSON olarak saklayalım
                var docList = new List<string>();
                foreach (var it in DocsList.Items)
                {
                    if (it is string s && !string.IsNullOrWhiteSpace(s)) docList.Add(s);
                }
                if (docList.Count > 0)
                {
                    try
                    {
                        var docSvc = new MesTechStok.Desktop.Services.DocumentStorageService();
                        var saved = new List<string>();
                        foreach (var path in docList)
                        {
                            var res = await docSvc.SaveAsync(c.Id, path);
                            if (!string.IsNullOrWhiteSpace(res)) saved.Add(res);
                        }
                        c.DocumentUrls = JsonSerializer.Serialize(saved);
                    }
                    catch
                    {
                        // Intentional: document save to storage — non-critical; customer record is still saved.
                    }
                }

                if (_editingCustomerId.HasValue)
                    await _customerService.UpdateCustomerAsync(c);
                else
                    await _customerService.CreateCustomerAsync(c);

                MesTechStok.Desktop.Utils.ToastManager.ShowSuccess("💾 Müşteri kaydedildi", "Müşteri");
                MesTechStok.Desktop.Utils.EventBus.PublishProductsChanged(null); // basit yenile tetikleyici
                Close();
            }
            catch (Exception ex)
            {
                MesTechStok.Desktop.Utils.ToastManager.ShowError($"Kayıt hatası: {ex.Message}", "Müşteri");
            }
            finally
            {
                _isSaving = false;
                if (btn != null) { btn.IsEnabled = true; btn.Content = originalContent; }
            }
        }

        private void CopyBillingToShipping_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(TxtBilling.Text)) TxtShipping.Text = TxtBilling.Text;
                if (!string.IsNullOrWhiteSpace(TxtCity.Text)) { /* şimdilik tek alan */ }
            }
            catch
            {
                // Intentional: UI event handler (copy billing to shipping) — UI element access during lifecycle.
            }
        }

        private void AddDocs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "Belgeler|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.png;*.jpg;*.jpeg|Tümü|*.*",
                    Multiselect = true
                };
                if (ofd.ShowDialog() == true)
                {
                    foreach (var f in ofd.FileNames) DocsList.Items.Add(f);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "{View} AddDocs handler error", nameof(CustomerEditPopup));
            }
        }

        private void RemoveDoc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sel = DocsList.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(sel)) return;
                var idx = DocsList.Items.IndexOf(sel);
                if (idx >= 0) DocsList.Items.RemoveAt(idx);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "{View} RemoveDoc handler error", nameof(CustomerEditPopup));
            }
        }

        private void OpenDoc_Click(object sender, RoutedEventArgs e)
        {
            var sel = DocsList.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(sel)) return;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = sel, UseShellExecute = true }); }
            catch (Exception ex) { /* Intentional: shell file open — OS may reject the file type or path. */ _logger?.LogWarning(ex, "{ViewName} - {Context}: {Message}", nameof(CustomerEditPopup), "Shell file open — OS may reject the file type or path", ex.Message); }
        }

        private void OpenDocFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var docSvc = new MesTechStok.Desktop.Services.DocumentStorageService();
                var folder = docSvc.GetCustomerFolder(_editingCustomerId ?? Guid.Empty);
                Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
            catch
            {
                // Intentional: shell folder open — Explorer launch may fail if path is unavailable.
            }
        }

        private List<string> ParseDocs(string? json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new List<string>();
                var list = JsonSerializer.Deserialize<List<string>>(json);
                return list ?? new List<string>();
            }
            catch (Exception ex) { /* Intentional: file listing fallback — return empty list on failure */ _logger?.LogWarning(ex, "{ViewName} - {Context}: {Message}", nameof(CustomerEditPopup), "File listing fallback — return empty list on failure", ex.Message); return new List<string>(); }
        }
    }
}


