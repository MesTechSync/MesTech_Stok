using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace MesTechStok.Desktop.Views
{
    public partial class CustomerEditPopup : Window
    {
        private readonly MesTechStok.Core.Services.Abstract.ICustomerService _customerService;
        private int? _editingCustomerId;

        public CustomerEditPopup()
        {
            InitializeComponent();
            var sp = MesTechStok.Desktop.App.ServiceProvider;
            _customerService = sp!.GetRequiredService<MesTechStok.Core.Services.Abstract.ICustomerService>();
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
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (e.Key == Key.S || e.Key == Key.Enter))
            {
                e.Handled = true;
                SaveAndClose_Click(this, new RoutedEventArgs());
            }
        }

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = (TxtName.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name)) { MesTechStok.Desktop.Utils.ToastManager.ShowWarning("Ad gerekli", "Müşteri"); return; }

                var c = new MesTechStok.Core.Data.Models.Customer
                {
                    Id = _editingCustomerId ?? 0,
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
                    catch { }
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
        }

        private void CopyBillingToShipping_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(TxtBilling.Text)) TxtShipping.Text = TxtBilling.Text;
                if (!string.IsNullOrWhiteSpace(TxtCity.Text)) { /* şimdilik tek alan */ }
            }
            catch { }
        }

        private void AddDocs_Click(object sender, RoutedEventArgs e)
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

        private void RemoveDoc_Click(object sender, RoutedEventArgs e)
        {
            var sel = DocsList.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(sel)) return;
            var idx = DocsList.Items.IndexOf(sel);
            if (idx >= 0) DocsList.Items.RemoveAt(idx);
        }

        private void OpenDoc_Click(object sender, RoutedEventArgs e)
        {
            var sel = DocsList.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(sel)) return;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = sel, UseShellExecute = true }); } catch { }
        }

        private void OpenDocFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var docSvc = new MesTechStok.Desktop.Services.DocumentStorageService();
                var folder = docSvc.GetCustomerFolder(_editingCustomerId ?? 0);
                Directory.CreateDirectory(folder);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = folder, UseShellExecute = true });
            }
            catch { }
        }

        private static List<string> ParseDocs(string? json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new List<string>();
                var list = JsonSerializer.Deserialize<List<string>>(json);
                return list ?? new List<string>();
            }
            catch { return new List<string>(); }
        }
    }
}


