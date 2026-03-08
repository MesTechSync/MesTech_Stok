using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.SaveCompanySettings;

namespace MesTechStok.Desktop.Views
{
    public partial class SettingsOverlayWindow : Window
    {
        public ObservableCollection<WarehouseItem> Warehouses { get; } = new();

        public SettingsOverlayWindow()
        {
            InitializeComponent();
            WarehouseList.ItemsSource = Warehouses;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Basit doğrulamalar
            if (string.IsNullOrWhiteSpace(CompanyName.Text))
            {
                MessageBox.Show("Firma adı zorunludur.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(CompanyEmail.Text))
            {
                var emailOk = Regex.IsMatch(CompanyEmail.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailOk)
                {
                    MessageBox.Show("E-posta adresi geçersiz.");
                    return;
                }
            }

            // Depo girdileri için basit doğrulamalar
            foreach (var wh in Warehouses)
            {
                if (string.IsNullOrWhiteSpace(wh.Name))
                {
                    MessageBox.Show("Depo/Şube adı boş olamaz.");
                    return;
                }
                if (!string.IsNullOrWhiteSpace(wh.Phone) && !Regex.IsMatch(wh.Phone, @"^[+0-9\-\s]{6,}$"))
                {
                    MessageBox.Show($"Telefon formatı geçersiz: {wh.Phone}");
                    return;
                }
            }

            try
            {
                var sp = Desktop.App.ServiceProvider;
                if (sp == null)
                {
                    MessageBox.Show("Servis sağlayıcı başlatılamadı.");
                    return;
                }

                var mediator = sp.GetRequiredService<IMediator>();

                var warehouseInputs = Warehouses.Select(w => new WarehouseInput
                {
                    Name = w.Name,
                    Address = w.Address,
                    City = w.City,
                    Phone = w.Phone,
                }).ToList();

                var result = await mediator.Send(new SaveCompanySettingsCommand(
                    CompanyName: CompanyName.Text.Trim(),
                    TaxNumber: CompanyTaxNumber.Text?.Trim(),
                    Phone: CompanyPhone.Text?.Trim(),
                    Email: CompanyEmail.Text?.Trim(),
                    Address: CompanyAddress.Text?.Trim(),
                    Warehouses: warehouseInputs));

                if (!result.IsSuccess)
                {
                    MessageBox.Show($"Ayarlar kaydedilemedi: {result.ErrorMessage}");
                    return;
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilemedi: {ex.Message}");
            }
        }

        private void AddWarehouseButton_Click(object sender, RoutedEventArgs e)
        {
            Warehouses.Add(new WarehouseItem { Name = "Yeni Depo", Address = "Adres", City = "Şehir", Phone = "+90" });
        }

        private void AddBranchButton_Click(object sender, RoutedEventArgs e)
        {
            Warehouses.Add(new WarehouseItem { Name = "Yeni Şube", Address = "Adres", City = "Şehir", Phone = "+90" });
        }
    }

    public class WarehouseItem
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
