using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

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

                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Tablo yoksa oluştur (SQLite)
                await EnsureCompanySettingsTableAsync(db);

                // CompanySettings tek kayıt politikası
                var settings = await db.CompanySettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new CompanySettings();
                    db.CompanySettings.Add(settings);
                }

                settings.CompanyName = CompanyName.Text.Trim();
                settings.TaxNumber = CompanyTaxNumber.Text?.Trim();
                settings.Phone = CompanyPhone.Text?.Trim();
                settings.Email = CompanyEmail.Text?.Trim();
                settings.Address = CompanyAddress.Text?.Trim();
                settings.ModifiedDate = DateTime.Now;

                // Warehouses: basit strateji – mevcutları temizle, yeniden ekle
                // Core Warehouse modelini kullanıyoruz
                var existing = db.Warehouses.ToList();
                if (existing.Any())
                {
                    db.Warehouses.RemoveRange(existing);
                }
                foreach (var w in Warehouses)
                {
                    db.Warehouses.Add(new Warehouse
                    {
                        Name = w.Name.Trim(),
                        Address = string.IsNullOrWhiteSpace(w.Address) ? null : w.Address.Trim(),
                        City = string.IsNullOrWhiteSpace(w.City) ? null : w.City.Trim(),
                        Phone = string.IsNullOrWhiteSpace(w.Phone) ? null : w.Phone.Trim(),
                        Type = "BRANCH",
                        IsActive = true
                    });
                }

                await db.SaveChangesAsync();

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilemedi: {ex.Message}");
            }
        }

        private static async Task EnsureCompanySettingsTableAsync(AppDbContext db)
        {
            try
            {
                var sql = @"CREATE TABLE IF NOT EXISTS ""CompanySettings"" (
                                ""Id"" SERIAL PRIMARY KEY,
                                ""CompanyName"" TEXT NOT NULL,
                                ""TaxNumber"" TEXT,
                                ""Phone"" TEXT,
                                ""Email"" TEXT,
                                ""Address"" TEXT,
                                ""CreatedDate"" TIMESTAMP NOT NULL,
                                ""ModifiedDate"" TIMESTAMP
                            );";
                await db.Database.ExecuteSqlRawAsync(sql);
            }
            catch
            {
                // yoksay: EF Migration'lar varsa tablo zaten vardır
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


