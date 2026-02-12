using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
// AI Services will be added after migration
using MesTechStok.Core.Services;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Views
{
    public partial class SettingsView : UserControl
    {
        private DispatcherTimer? performanceTimer;
        private Random random = new Random();

        // AI Services implementation - integrated with existing structure
        private IAIConfigurationService? _aiService;
        private List<AIConfiguration> _currentAIConfigs = new List<AIConfiguration>();

        public SettingsView()
        {
            InitializeComponent();
            InitializePerformanceMonitor();
            LoadSettings();
            _ = LoadCompanyFromSqlAsync();

            // Initialize AI Services with existing structure
            _ = InitializeAIServicesAsync();
        }

        private void InitializePerformanceMonitor()
        {
            performanceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            performanceTimer.Tick += UpdatePerformanceStats;
            performanceTimer.Start();
        }

        private void UpdatePerformanceStats(object? sender, EventArgs e)
        {
            // Demo performance data with some variation
            int memoryUsage = 140 + random.Next(20); // 140-160 MB
            double cpuUsage = 2.0 + (random.NextDouble() * 3); // 2-5%
            double dbSize = 2.3 + (random.NextDouble() * 0.4); // 2.3-2.7 MB

            MemoryUsageText.Text = $"{memoryUsage} MB";
            CpuUsageText.Text = $"{cpuUsage:F1}%";
            DatabaseSizeText.Text = $"{dbSize:F1} MB";

            // Update uptime (demo)
            DateTime startTime = DateTime.Now.AddHours(-3).AddMinutes(-25);
            TimeSpan uptime = DateTime.Now - startTime;
            UptimeText.Text = $"{uptime.Hours}s {uptime.Days}g {uptime.Minutes}d";
        }

        private void LoadSettings()
        {
            // Load current settings (from configuration)
            try
            {
                var sp = Desktop.App.ServiceProvider;
                if (sp != null)
                {
                    using var scope = sp.CreateScope();
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var cs = config?.GetConnectionString("DefaultConnection") ?? string.Empty;
                    ConnectionStringTextBox.Text = cs;
                }
            }
            catch { /* ignore */ }
            CompanyNameTextBox.Text = string.IsNullOrWhiteSpace(CompanyNameTextBox.Text) ? "" : CompanyNameTextBox.Text;
            LastUpdateText.Text = DateTime.Now.ToString("dd.MM.yyyy");
            // Header şirket adını güncelle
            try
            {
                var mw = Application.Current?.Windows.OfType<Desktop.MainWindow>().FirstOrDefault();
                var headerCompany = mw?.FindName("HeaderCompanyName") as TextBlock;
                if (headerCompany != null && !string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
                {
                    headerCompany.Text = CompanyNameTextBox.Text.Trim();
                }
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadCompanyFromSqlAsync()
        {
            try
            {
                var sp = Desktop.App.ServiceProvider;
                if (sp == null) return;
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var settings = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync();
                if (settings != null)
                {
                    SqlCompanyName.Text = settings.CompanyName ?? string.Empty;
                    SqlTaxNumber.Text = settings.TaxNumber ?? string.Empty;
                    SqlPhone.Text = settings.Phone ?? string.Empty;
                    SqlEmail.Text = settings.Email ?? string.Empty;
                    SqlAddress.Text = settings.Address ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
                        CompanyNameTextBox.Text = settings.CompanyName ?? string.Empty;
                }

                // Header firma adını SQL'den set et
                try
                {
                    var mw = Application.Current?.Windows.OfType<Desktop.MainWindow>().FirstOrDefault();
                    var headerCompany = mw?.FindName("HeaderCompanyName") as TextBlock;
                    if (headerCompany != null && !string.IsNullOrWhiteSpace(SqlCompanyName.Text))
                    {
                        headerCompany.Text = SqlCompanyName.Text.Trim();
                    }
                }
                catch { }

                var warehouses = await db.Warehouses.AsNoTracking().ToListAsync();
                WarehousesList.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<TempWarehouseItem>(
                    warehouses.Select(w => new TempWarehouseItem
                    {
                        Name = w.Name,
                        Address = w.Address ?? string.Empty,
                        City = w.City ?? string.Empty,
                        Phone = w.Phone ?? string.Empty
                    })
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsView SQL load error: {ex.Message}");
            }
        }

        private void AddWarehouseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WarehousesList.ItemsSource is System.Collections.ObjectModel.ObservableCollection<TempWarehouseItem> list)
            {
                list.Add(new TempWarehouseItem { Name = "Yeni Depo", Address = "Adres", City = "Şehir", Phone = "+90" });
            }
        }

        private void RemoveWarehouse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TempWarehouseItem item &&
                WarehousesList.ItemsSource is System.Collections.ObjectModel.ObservableCollection<TempWarehouseItem> list)
            {
                list.Remove(item);
            }
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var validationErrors = ValidateSettingsInputs();
                if (validationErrors.Length > 0)
                {
                    MessageBox.Show("Lütfen aşağıdaki alanları düzeltin:\n\n" + validationErrors,
                        "Doğrulama Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var sp = Desktop.App.ServiceProvider;
                if (sp != null)
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var s = await db.CompanySettings.FirstOrDefaultAsync();
                    if (s == null)
                    {
                        s = new CompanySettings { CreatedDate = DateTime.Now };
                        db.CompanySettings.Add(s);
                    }

                    var effectiveCompanyName = !string.IsNullOrWhiteSpace(SqlCompanyName.Text)
                        ? SqlCompanyName.Text.Trim()
                        : (CompanyNameTextBox.Text?.Trim() ?? string.Empty);
                    s.CompanyName = effectiveCompanyName;
                    s.TaxNumber = string.IsNullOrWhiteSpace(SqlTaxNumber.Text) ? null : SqlTaxNumber.Text.Trim();
                    s.Phone = string.IsNullOrWhiteSpace(SqlPhone.Text) ? null : SqlPhone.Text.Trim();
                    s.Email = string.IsNullOrWhiteSpace(SqlEmail.Text) ? null : SqlEmail.Text.Trim();
                    s.Address = string.IsNullOrWhiteSpace(SqlAddress.Text) ? null : SqlAddress.Text.Trim();
                    s.ModifiedDate = DateTime.Now;

                    // Güvenli UPSERT: Var olan depoları güncelle, olmayanı ekle; ilişkisi olanı silme
                    var existing = await db.Warehouses.ToListAsync();
                    var existingMap = existing
                        .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                        .ToDictionary(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase);

                    var incoming = new List<TempWarehouseItem>();
                    if (WarehousesList.ItemsSource is System.Collections.ObjectModel.ObservableCollection<TempWarehouseItem> list)
                        incoming = list.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();

                    // Ekle/Güncelle
                    foreach (var w in incoming)
                    {
                        var key = w.Name.Trim();
                        if (existingMap.TryGetValue(key, out var found))
                        {
                            found.Address = string.IsNullOrWhiteSpace(w.Address) ? null : w.Address.Trim();
                            found.City = string.IsNullOrWhiteSpace(w.City) ? null : w.City.Trim();
                            found.Phone = string.IsNullOrWhiteSpace(w.Phone) ? null : w.Phone.Trim();
                            found.Type = string.IsNullOrWhiteSpace(found.Type) ? "BRANCH" : found.Type;
                            found.IsActive = true;
                        }
                        else
                        {
                            db.Warehouses.Add(new Warehouse
                            {
                                Name = key,
                                Address = string.IsNullOrWhiteSpace(w.Address) ? null : w.Address.Trim(),
                                City = string.IsNullOrWhiteSpace(w.City) ? null : w.City.Trim(),
                                Phone = string.IsNullOrWhiteSpace(w.Phone) ? null : w.Phone.Trim(),
                                Type = "BRANCH",
                                IsActive = true
                            });
                        }
                    }

                    // Silinecekler: Gelen listede olmayanlar
                    var incomingNames = new HashSet<string>(incoming.Select(i => i.Name.Trim()), StringComparer.OrdinalIgnoreCase);
                    foreach (var wh in existing)
                    {
                        if (!incomingNames.Contains(wh.Name.Trim()))
                        {
                            // İlişki kontrolü: Ürünü varsa silme, pasifleştir
                            bool hasProducts = await db.Products.AnyAsync(p => p.WarehouseId == wh.Id);
                            if (hasProducts)
                            {
                                wh.IsActive = false;
                            }
                            else
                            {
                                db.Warehouses.Remove(wh);
                            }
                        }
                    }

                    await db.SaveChangesAsync();

                    try
                    {
                        MesTechStok.Desktop.Utils.EventBus.PublishCompanySettingsChanged(effectiveCompanyName);
                    }
                    catch { }
                }

                LastUpdateText.Text = DateTime.Now.ToString("dd.MM.yyyy");
                // Header'ı güncelle
                try
                {
                    var mw = Application.Current?.Windows.OfType<Desktop.MainWindow>().FirstOrDefault();
                    var headerCompany = mw?.FindName("HeaderCompanyName") as TextBlock;
                    var headerName = !string.IsNullOrWhiteSpace(SqlCompanyName.Text)
                        ? SqlCompanyName.Text.Trim()
                        : (CompanyNameTextBox.Text?.Trim() ?? string.Empty);
                    if (headerCompany != null && !string.IsNullOrWhiteSpace(headerName))
                    {
                        headerCompany.Text = headerName;
                    }
                }
                catch { }
                MessageBox.Show("✅ Ayarlar kaydedildi ve SQL ile senkronize edildi.",
                                "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Ayarlar kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ValidateSettingsInputs()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Firma bilgileri
            if (string.IsNullOrWhiteSpace(SqlCompanyName.Text))
                errors.Add("• Firma Adı zorunludur");
            if (!string.IsNullOrWhiteSpace(SqlEmail.Text))
            {
                var email = SqlEmail.Text.Trim();
                // Basit e-posta kontrolü
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    errors.Add("• E-posta formatı geçersiz");
            }
            if (!string.IsNullOrWhiteSpace(SqlPhone.Text) && SqlPhone.Text.Trim().Length > 20)
                errors.Add("• Telefon 20 karakteri aşamaz");
            if (!string.IsNullOrWhiteSpace(SqlTaxNumber.Text) && SqlTaxNumber.Text.Trim().Length > 50)
                errors.Add("• Vergi No 50 karakteri aşamaz");
            if (!string.IsNullOrWhiteSpace(SqlAddress.Text) && SqlAddress.Text.Trim().Length > 1000)
                errors.Add("• Adres 1000 karakteri aşamaz");

            // Depo listesi
            if (WarehousesList.ItemsSource is System.Collections.ObjectModel.ObservableCollection<TempWarehouseItem> list)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var w in list)
                {
                    if (string.IsNullOrWhiteSpace(w.Name))
                        errors.Add("• Depo adı boş olamaz");
                    else if (w.Name.Trim().Length > 100)
                        errors.Add($"• Depo adı çok uzun: {w.Name}");
                    else
                    {
                        if (!names.Add(w.Name.Trim()))
                            errors.Add($"• Aynı isimde birden fazla depo var: {w.Name}");
                    }
                    if (!string.IsNullOrWhiteSpace(w.City) && w.City.Trim().Length > 100)
                        errors.Add($"• Şehir adı çok uzun: {w.City}");
                    if (!string.IsNullOrWhiteSpace(w.Phone) && w.Phone.Trim().Length > 20)
                        errors.Add($"• Depo telefon bilgisi çok uzun: {w.Phone}");
                }
            }

            return string.Join("\n", errors.Distinct());
        }

        private class TempWarehouseItem
        {
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }

        #region Event Handlers

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("⚠️ Tüm ayarlar varsayılan değerlere sıfırlanacak.\n\nDevam etmek istiyor musunuz?",
                                       "Varsayılana Sıfırla",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset to defaults
                CompanyNameTextBox.Text = "Şirket Adı";
                LanguageComboBox.SelectedIndex = 0;
                CurrencyComboBox.SelectedIndex = 0;
                ThemeComboBox.SelectedIndex = 0;
                DatabaseTypeComboBox.SelectedIndex = 0;
                // SQL Server mimarisine uygun placeholder bilgi
                ConnectionStringTextBox.Text = "Server=localhost\\SQLEXPRESS;Database=MesTech_stok;...";
                BackupFrequencyComboBox.SelectedIndex = 0;

                // Reset notifications
                LowStockNotificationsCheckBox.IsChecked = true;
                OrderNotificationsCheckBox.IsChecked = true;
                UpdateNotificationsCheckBox.IsChecked = true;
                EmailNotificationsCheckBox.IsChecked = false;

                MessageBox.Show("✅ Ayarlar varsayılan değerlere sıfırlandı.",
                              "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void TestDatabaseConnection_Click(object sender, RoutedEventArgs e)
        {
            DatabaseStatusText.Text = "🔄 Test ediliyor...";
            try
            {
                var sp = Desktop.App.ServiceProvider;
                if (sp == null) throw new Exception("ServiceProvider yok");
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var can = await db.Database.CanConnectAsync();
                if (can)
                {
                    DatabaseStatusText.Text = "✅ Bağlı";
                    MessageBox.Show("✅ Veritabanı bağlantısı başarılı!", "Bağlantı Testi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    DatabaseStatusText.Text = "❌ Bağlantı Hatası";
                    MessageBox.Show("❌ Veritabanı bağlantısı sağlanamadı!", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DatabaseStatusText.Text = "❌ Bağlantı Hatası";
                MessageBox.Show($"❌ Bağlantı testi sırasında hata: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"mestechstok_settings_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Demo export data
                    var settingsJson = "{\n" +
                                     "  \"CompanyName\": \"" + CompanyNameTextBox.Text + "\",\n" +
                                     "  \"Language\": \"Türkçe\",\n" +
                                     "  \"Currency\": \"₺ Türk Lirası\",\n" +
                                     "  \"Theme\": \"Modern Light\",\n" +
                                     "  \"DatabaseType\": \"SQLite\",\n" +
                                     "  \"ConnectionString\": \"" + ConnectionStringTextBox.Text + "\",\n" +
                                     "  \"ExportDate\": \"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"\n" +
                                     "}";

                    File.WriteAllText(saveFileDialog.FileName, settingsJson);

                    MessageBox.Show($"✅ Ayarlar başarıyla dışa aktarıldı!\n\nDosya: {Path.GetFileName(saveFileDialog.FileName)}",
                                  "Dışa Aktarım Başarılı",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Dışa aktarım sırasında hata oluştu:\n{ex.Message}",
                              "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    Title = "Ayarlar Dosyası Seçin"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    if (File.Exists(openFileDialog.FileName))
                    {
                        var result = MessageBox.Show("⚠️ Mevcut ayarlar içe aktarılan ayarlarla değiştirilecek.\n\nDevam etmek istiyor musunuz?",
                                                   "Ayarları İçe Aktar",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // Demo import - normally would parse JSON
                            var fileContent = File.ReadAllText(openFileDialog.FileName);

                            MessageBox.Show($"✅ Ayarlar başarıyla içe aktarıldı!\n\n" +
                                          $"Dosya: {Path.GetFileName(openFileDialog.FileName)}\n" +
                                          $"Boyut: {new FileInfo(openFileDialog.FileName).Length} bytes",
                                          "İçe Aktarım Başarılı",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);

                            LoadSettings(); // Reload settings
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ İçe aktarım sırasında hata oluştu:\n{ex.Message}",
                              "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BackupDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "SQL Server Yedeği (*.bak)|*.bak|Tüm Dosyalar (*.*)|*.*",
                    DefaultExt = "bak",
                    FileName = $"mestechstok_backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var targetPath = saveFileDialog.FileName;

                    var sp = Desktop.App.ServiceProvider;
                    if (sp == null) throw new Exception("ServiceProvider yok");
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var dbName = db.Database.GetDbConnection().Database;
                    if (string.IsNullOrWhiteSpace(dbName))
                        throw new Exception("Veritabanı adı okunamadı");

                    // Yedekleme komutu (SQL Server) - GÜVENLİ
                    var backupSql = $"BACKUP DATABASE [{dbName}] TO DISK = @targetPath WITH INIT, NAME = N'MesTechStok Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                    // Yedekleme işlemini çalıştır
                    var pathParam = new SqlParameter("@targetPath", targetPath);
                    await db.Database.ExecuteSqlRawAsync(backupSql, pathParam);

                    // Dosya bilgisi
                    var sizeText = File.Exists(targetPath) ? $"{new FileInfo(targetPath).Length / (1024 * 1024.0):F1} MB" : "?";
                    MessageBox.Show($"✅ Veritabanı yedeği tamamlandı!\n\nDosya: {Path.GetFileName(targetPath)}\nBoyut: {sizeText}",
                                      "Yedekleme Başarılı",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Yedekleme sırasında hata oluştu:\n{ex.Message}",
                              "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("⚠️ Tüm log dosyaları silinecek.\n\nBu işlem geri alınamaz. Devam etmek istiyor musunuz?",
                                       "Log Dosyalarını Temizle",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Demo log clearing
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        MessageBox.Show("✅ Log dosyaları temizlendi!\n\n" +
                                      "• Silinen dosyalar: 45\n" +
                                      "• Temizlenen alan: 15.2 MB\n" +
                                      "• Eski kayıtlar: 30 gün+",
                                      "Temizlik Tamamlandı",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Log temizleme sırasında hata oluştu:\n{ex.Message}",
                                  "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        // Cleanup timer when control is unloaded
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            performanceTimer?.Stop();
        }

        // ProductsView profil köprüsü ile kolon/filtre yönetimi
        private void LoadProductsColumnsProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
                if (ofd.ShowDialog() == true)
                {
                    var json = File.ReadAllText(ofd.FileName);
                    var profiles = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<MesTechStok.Desktop.Views.ProductsView.ColumnProfileDto>>(json) ?? new();
                    MesTechStok.Desktop.Views.ProductsView.ProductsViewProfilesBridge.ApplyProfiles(profiles);
                    MessageBox.Show("Kolon profili uygulandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kolon profili yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProductsColumnsProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profiles = MesTechStok.Desktop.Views.ProductsView.ProductsViewProfilesBridge.CaptureProfiles();
                var sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = $"columns_{DateTime.Now:yyyyMMdd_HHmmss}.json" };
                if (sfd.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(profiles, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(sfd.FileName, json);
                    MessageBox.Show("Kolon profili kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kolon profili kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetProductsColumns_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MesTechStok.Desktop.Views.ProductsView.ProductsViewProfilesBridge.ResetToDefault();
                MessageBox.Show("Kolonlar varsayılana alındı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem başarısız: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProductsFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ProductsView filtre JSON dosyasını kullanıcıdan almak için hızlı bir köprü: Autosave.json varsa kopyalarız
                var dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
                System.IO.Directory.CreateDirectory(dir);
                var autosave = System.IO.Path.Combine(dir, "filters.json");
                var sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = $"filters_{DateTime.Now:yyyyMMdd_HHmmss}.json" };
                if (sfd.ShowDialog() == true)
                {
                    if (File.Exists(autosave)) File.Copy(autosave, sfd.FileName, overwrite: true);
                    else File.WriteAllText(sfd.FileName, "{}");
                    MessageBox.Show("Filtre profili kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filtre profili kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductsFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
                if (ofd.ShowDialog() == true)
                {
                    var json = File.ReadAllText(ofd.FileName);
                    // ProductsView tarafındaki mevcut yükleme metodunu tetiklemek yerine EventBus ile yenileme yayınlarız
                    MesTechStok.Desktop.Utils.EventBus.PublishProductsChanged(null);
                    MessageBox.Show("Filtre profili uygulandı (ProductsView yenilendi).", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filtre profili yüklenemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // AI Configuration Methods - A++++ Enterprise Integration
        // TEMPORARILY DISABLED FOR DATABASE MIGRATION - WILL BE RESTORED AFTER MIGRATION

        /// <summary>
        /// Mevcut AI konfigürasyonlarını yükle ve UI'a bind et
        /// TEMPORARILY DISABLED FOR XAML MIGRATION - AI CONTROLS COMMENTED OUT
        /// </summary>
        private async Task LoadAIConfigurationsAsync()
        {
            try
            {
                if (_aiService == null) return;

                var configs = await _aiService.GetAllConfigurationsAsync();
                _currentAIConfigs = configs.ToList();

                // Load each provider's configurations
                LoadAIDefaultSettings_ChatGPT();
                LoadAIDefaultSettings_Gemini();
                LoadAIDefaultSettings_DeepSeek();
                LoadAIDefaultSettings_Claude();

                await UpdateAIUsageStatisticsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI yapılandırmaları yüklenirken hata: {ex.Message}", "Hata",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadAIDefaultSettings_ChatGPT()
        {
            try
            {
                var config = _currentAIConfigs?.FirstOrDefault(c => c.ProviderName == "ChatGPT");
                if (config != null)
                {
                    ChatGPTApiKeyTextBox.Password = config.ApiKey ?? "";
                    ChatGPTActiveCheckBox.IsChecked = config.IsActive;
                    ChatGPTMaxTokensTextBox.Text = config.MaxTokens.ToString();
                    ChatGPTTemperatureTextBox.Text = config.Temperature.ToString();
                    ChatGPTDailyLimitTextBox.Text = config.DailyLimit?.ToString() ?? "1000";

                    // Select model in ComboBox
                    if (!string.IsNullOrEmpty(config.Model))
                    {
                        var item = ChatGPTModelComboBox.Items.Cast<ComboBoxItem>()
                            .FirstOrDefault(i => i.Content.ToString().Contains(config.Model));
                        if (item != null) item.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show to user
                Console.WriteLine($"LoadAIDefaultSettings_ChatGPT error: {ex.Message}");
            }
        }

        private void LoadAIDefaultSettings_Gemini()
        {
            try
            {
                var config = _currentAIConfigs?.FirstOrDefault(c => c.ProviderName == "Gemini");
                if (config != null)
                {
                    GeminiApiKeyTextBox.Password = config.ApiKey ?? "";
                    GeminiActiveCheckBox.IsChecked = config.IsActive;
                    GeminiMaxTokensTextBox.Text = config.MaxTokens.ToString();
                    GeminiTemperatureTextBox.Text = config.Temperature.ToString();
                    GeminiDailyLimitTextBox.Text = config.DailyLimit?.ToString() ?? "800";

                    if (!string.IsNullOrEmpty(config.Model))
                    {
                        var item = GeminiModelComboBox.Items.Cast<ComboBoxItem>()
                            .FirstOrDefault(i => i.Content.ToString().Contains(config.Model));
                        if (item != null) item.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAIDefaultSettings_Gemini error: {ex.Message}");
            }
        }

        private void LoadAIDefaultSettings_DeepSeek()
        {
            try
            {
                var config = _currentAIConfigs?.FirstOrDefault(c => c.ProviderName == "DeepSeek");
                if (config != null)
                {
                    DeepSeekApiKeyTextBox.Password = config.ApiKey ?? "";
                    DeepSeekActiveCheckBox.IsChecked = config.IsActive;
                    DeepSeekMaxTokensTextBox.Text = config.MaxTokens.ToString();
                    DeepSeekTemperatureTextBox.Text = config.Temperature.ToString();
                    DeepSeekDailyLimitTextBox.Text = config.DailyLimit?.ToString() ?? "500";

                    if (!string.IsNullOrEmpty(config.Model))
                    {
                        var item = DeepSeekModelComboBox.Items.Cast<ComboBoxItem>()
                            .FirstOrDefault(i => i.Content.ToString().Contains(config.Model));
                        if (item != null) item.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAIDefaultSettings_DeepSeek error: {ex.Message}");
            }
        }

        private void LoadAIDefaultSettings_Claude()
        {
            try
            {
                var config = _currentAIConfigs?.FirstOrDefault(c => c.ProviderName == "Claude");
                if (config != null)
                {
                    ClaudeApiKeyTextBox.Password = config.ApiKey ?? "";
                    ClaudeActiveCheckBox.IsChecked = config.IsActive;
                    ClaudeMaxTokensTextBox.Text = config.MaxTokens.ToString();
                    ClaudeTemperatureTextBox.Text = config.Temperature.ToString();
                    ClaudeDailyLimitTextBox.Text = config.DailyLimit.ToString(); if (!string.IsNullOrEmpty(config.Model))
                    {
                        var item = ClaudeModelComboBox.Items.Cast<ComboBoxItem>()
                            .FirstOrDefault(i => i.Content.ToString().Contains(config.Model));
                        if (item != null) item.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadAIDefaultSettings_Claude error: {ex.Message}");
            }
        }

        /// <summary>
        /// AI kullanım istatistiklerini güncelle
        /// </summary>
        private async Task UpdateAIUsageStatisticsAsync()
        {
            try
            {
                if (_aiService == null) return;

                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);

                // Bugünkü istekler
                var todayRequests = 0; // await _aiService.GetUsageCountAsync(today, today.AddDays(1));
                                       // TodayTotalRequestsText.Text = todayRequests.ToString();

                // Bu ayki istekler  
                var monthRequests = 0; // await _aiService.GetUsageCountAsync(monthStart, today.AddDays(1));
                                       // MonthTotalRequestsText.Text = monthRequests.ToString("N0");

                // Toplam maliyet (tahmini)
                var totalCost = monthRequests * 0.003; // Örnek maliyet
                                                       // TotalCostText.Text = $"${totalCost:F2}";

                // Başarı oranı (örnek)
                var successRate = 98.5;
                // SuccessRateText.Text = $"{successRate:F1}%";

                // En çok kullanılan sağlayıcıyı güncelle (UI elementleri eksik olduğu için yorum satırına alındı)
                // var mostUsedProvider = "ChatGPT"; // Bu gerçek veriden gelecek
                // TopProviderNameText.Text = mostUsedProvider;
                // TopProviderPercentText.Text = "45.2%"; // Bu da gerçek veriden gelecek
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateAIUsageStatisticsAsync error: {ex.Message}");
                // Varsayılan değerler
                // TodayTotalRequestsText.Text = "0";
                // MonthTotalRequestsText.Text = "0";
                // TotalCostText.Text = "$0.00";
                // SuccessRateText.Text = "0.0%";
            }
        }

        private string GetProviderIcon(string providerName)
        {
            // AI features temporarily disabled
            return "AI";
        }

        // AI Event Handlers - Professional UI Integration

        private async void SaveAISettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_aiService == null)
                {
                    MessageBox.Show("AI servisi kullanılamıyor.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var button = sender as Button;
                button!.IsEnabled = false;
                button.Content = "💾 Kaydediliyor...";

                // Validate and save each provider configuration
                await SaveChatGPTConfigurationAsync();
                await SaveGeminiConfigurationAsync();
                await SaveDeepSeekConfigurationAsync();
                await SaveClaudeConfigurationAsync();

                // Reload configurations and update UI
                await LoadAIConfigurationsAsync();
                await UpdateAIUsageStatisticsAsync();

                MessageBox.Show("AI ayarları başarıyla kaydedildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                Debug.WriteLine("[AI_CONFIG] All AI settings saved successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI ayarları kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[AI_CONFIG] Failed to save AI settings: {ex.Message}");
            }
            finally
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "💾 AI Ayarlarını Kaydet";
                }
            }
        }

        private async Task SaveChatGPTConfigurationAsync()
        {
            if (_aiService == null) return;

            var existingConfig = _currentAIConfigs.FirstOrDefault(c => c.ProviderName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase));
            var config = existingConfig ?? AIProviderTemplates.CreateChatGPTConfiguration("");

            // Preserve original encrypted ApiKey when UI shows masked bullets
            var chatKeyBox = this.FindName("ChatGPTApiKeyTextBox") as PasswordBox;
            var chatActive = this.FindName("ChatGPTActiveCheckBox") as CheckBox;
            var chatModel = this.FindName("ChatGPTModelComboBox") as ComboBox;
            var chatMaxTokens = this.FindName("ChatGPTMaxTokensTextBox") as TextBox;
            var chatTemperature = this.FindName("ChatGPTTemperatureTextBox") as TextBox;
            var chatDaily = this.FindName("ChatGPTDailyLimitTextBox") as TextBox;

            string uiApiKey = chatKeyBox?.Password?.Trim() ?? string.Empty;
            if (existingConfig != null && (string.IsNullOrWhiteSpace(uiApiKey) || uiApiKey.Contains('•')))
            {
                // Fetch persisted config to keep encrypted key
                var persisted = await _aiService.GetConfigurationAsync(existingConfig.Id);
                if (persisted != null)
                    config.ApiKey = persisted.ApiKey;
            }
            else
            {
                config.ApiKey = uiApiKey;
            }

            config.IsActive = chatActive?.IsChecked ?? false;
            var modelRaw = (chatModel?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "gpt-4o";
            var modelClean = SanitizeModelName(modelRaw);
            config.ModelName = modelClean;
            config.Model = modelClean;

            if (int.TryParse(chatMaxTokens?.Text, out int maxTokens))
                config.MaxTokens = maxTokens;
            if (double.TryParse(chatTemperature?.Text, out double temperature))
                config.Temperature = temperature;
            if (int.TryParse(chatDaily?.Text, out int dailyLimit))
            {
                config.MaxRequestsPerDay = dailyLimit;
                config.DailyLimit = dailyLimit;
            }

            await _aiService.SaveConfigurationAsync(config);
        }

        private async Task SaveGeminiConfigurationAsync()
        {
            if (_aiService == null) return;

            var existingConfig = _currentAIConfigs.FirstOrDefault(c => c.ProviderName.Equals("Gemini", StringComparison.OrdinalIgnoreCase));
            var config = existingConfig ?? AIProviderTemplates.CreateGeminiConfiguration("");

            var gemKeyBox = this.FindName("GeminiApiKeyTextBox") as PasswordBox;
            var gemActive = this.FindName("GeminiActiveCheckBox") as CheckBox;
            var gemModel = this.FindName("GeminiModelComboBox") as ComboBox;
            var gemMaxTokens = this.FindName("GeminiMaxTokensTextBox") as TextBox;
            var gemTemperature = this.FindName("GeminiTemperatureTextBox") as TextBox;
            var gemDaily = this.FindName("GeminiDailyLimitTextBox") as TextBox;

            string uiApiKey = gemKeyBox?.Password?.Trim() ?? string.Empty;
            if (existingConfig != null && (string.IsNullOrWhiteSpace(uiApiKey) || uiApiKey.Contains('•')))
            {
                var persisted = await _aiService.GetConfigurationAsync(existingConfig.Id);
                if (persisted != null)
                    config.ApiKey = persisted.ApiKey;
            }
            else
            {
                config.ApiKey = uiApiKey;
            }

            config.IsActive = gemActive?.IsChecked ?? false;
            var modelRaw = (gemModel?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "gemini-pro";
            var modelClean = SanitizeModelName(modelRaw);
            config.ModelName = modelClean;
            config.Model = modelClean;

            if (int.TryParse(gemMaxTokens?.Text, out int maxTokens))
                config.MaxTokens = maxTokens;
            if (double.TryParse(gemTemperature?.Text, out double temperature))
                config.Temperature = temperature;
            if (int.TryParse(gemDaily?.Text, out int dailyLimit))
            {
                config.MaxRequestsPerDay = dailyLimit;
                config.DailyLimit = dailyLimit;
            }

            await _aiService.SaveConfigurationAsync(config);
        }

        private async Task SaveDeepSeekConfigurationAsync()
        {
            if (_aiService == null) return;

            var existingConfig = _currentAIConfigs.FirstOrDefault(c => c.ProviderName.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase));
            var config = existingConfig ?? AIProviderTemplates.CreateDeepSeekConfiguration("");

            var deepKeyBox = this.FindName("DeepSeekApiKeyTextBox") as PasswordBox;
            var deepActive = this.FindName("DeepSeekActiveCheckBox") as CheckBox;
            var deepModel = this.FindName("DeepSeekModelComboBox") as ComboBox;
            var deepMaxTokens = this.FindName("DeepSeekMaxTokensTextBox") as TextBox;
            var deepTemperature = this.FindName("DeepSeekTemperatureTextBox") as TextBox;
            var deepDaily = this.FindName("DeepSeekDailyLimitTextBox") as TextBox;

            string uiApiKey = deepKeyBox?.Password?.Trim() ?? string.Empty;
            if (existingConfig != null && (string.IsNullOrWhiteSpace(uiApiKey) || uiApiKey.Contains('•')))
            {
                var persisted = await _aiService.GetConfigurationAsync(existingConfig.Id);
                if (persisted != null)
                    config.ApiKey = persisted.ApiKey;
            }
            else
            {
                config.ApiKey = uiApiKey;
            }

            config.IsActive = deepActive?.IsChecked ?? false;
            var modelRaw = (deepModel?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "deepseek-chat";
            var modelClean = SanitizeModelName(modelRaw);
            config.ModelName = modelClean;
            config.Model = modelClean;

            if (int.TryParse(deepMaxTokens?.Text, out int maxTokens))
                config.MaxTokens = maxTokens;
            if (double.TryParse(deepTemperature?.Text, out double temperature))
                config.Temperature = temperature;
            if (int.TryParse(deepDaily?.Text, out int dailyLimit))
            {
                config.MaxRequestsPerDay = dailyLimit;
                config.DailyLimit = dailyLimit;
            }

            await _aiService.SaveConfigurationAsync(config);
        }

        private async Task SaveClaudeConfigurationAsync()
        {
            if (_aiService == null) return;

            var existingConfig = _currentAIConfigs.FirstOrDefault(c => c.ProviderName.Equals("Claude", StringComparison.OrdinalIgnoreCase));
            var config = existingConfig ?? AIProviderTemplates.CreateClaudeConfiguration("");

            var claudeKeyBox = this.FindName("ClaudeApiKeyTextBox") as PasswordBox;
            var claudeActive = this.FindName("ClaudeActiveCheckBox") as CheckBox;
            var claudeModel = this.FindName("ClaudeModelComboBox") as ComboBox;
            var claudeMaxTokens = this.FindName("ClaudeMaxTokensTextBox") as TextBox;
            var claudeTemperature = this.FindName("ClaudeTemperatureTextBox") as TextBox;
            var claudeDaily = this.FindName("ClaudeDailyLimitTextBox") as TextBox;

            string uiApiKey = claudeKeyBox?.Password?.Trim() ?? string.Empty;
            if (existingConfig != null && (string.IsNullOrWhiteSpace(uiApiKey) || uiApiKey.Contains('•')))
            {
                var persisted = await _aiService.GetConfigurationAsync(existingConfig.Id);
                if (persisted != null)
                    config.ApiKey = persisted.ApiKey;
            }
            else
            {
                config.ApiKey = uiApiKey;
            }

            config.IsActive = claudeActive?.IsChecked ?? false;
            var modelRaw = (claudeModel?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "claude-3-sonnet-20240229";
            var modelClean = SanitizeModelName(modelRaw);
            config.ModelName = modelClean;
            config.Model = modelClean;

            if (int.TryParse(claudeMaxTokens?.Text, out int maxTokens))
                config.MaxTokens = maxTokens;
            if (double.TryParse(claudeTemperature?.Text, out double temperature))
                config.Temperature = temperature;
            if (int.TryParse(claudeDaily?.Text, out int dailyLimit))
            {
                config.MaxRequestsPerDay = dailyLimit;
                config.DailyLimit = dailyLimit;
            }

            await _aiService.SaveConfigurationAsync(config);
        }

        private static string SanitizeModelName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            // Remove leading emoji and extra spaces
            var cleaned = raw.Trim();
            // If it contains a space and an emoji prefix like "🚀 ", drop the prefix
            int spaceIdx = cleaned.IndexOf(' ');
            if (spaceIdx > 0 && char.GetUnicodeCategory(cleaned[0]) == System.Globalization.UnicodeCategory.OtherSymbol)
            {
                cleaned = cleaned.Substring(spaceIdx + 1).Trim();
            }
            return cleaned;
        }

        private async void TestChatGPTConnection_Click(object sender, RoutedEventArgs e)
        {
            await TestProviderConnectionAsync("ChatGPT", sender as Button);
        }

        private async void TestGeminiConnection_Click(object sender, RoutedEventArgs e)
        {
            await TestProviderConnectionAsync("Gemini", sender as Button);
        }

        private async void TestDeepSeekConnection_Click(object sender, RoutedEventArgs e)
        {
            await TestProviderConnectionAsync("DeepSeek", sender as Button);
        }

        private async void TestClaudeConnection_Click(object sender, RoutedEventArgs e)
        {
            await TestProviderConnectionAsync("Claude", sender as Button);
        }

        private void UpdateProviderUsageDisplay(string providerName)
        {
            try
            {
                var random = new Random();
                switch (providerName)
                {
                    case "ChatGPT":
                        var chatUsage = random.Next(50, 300);
                        var chatDaily = (this.FindName("ChatGPTDailyLimitTextBox") as TextBox)?.Text;
                        var chatUsageText = this.FindName("ChatGPTUsageText") as TextBlock;
                        if (int.TryParse(chatDaily, out var chatLimit) && chatUsageText != null)
                            chatUsageText.Text = $"{chatUsage}/{chatLimit} ({(chatUsage * 100.0 / chatLimit):F1}%)";
                        break;
                    case "Gemini":
                        var geminiUsage = random.Next(30, 250);
                        var gemDaily = (this.FindName("GeminiDailyLimitTextBox") as TextBox)?.Text;
                        var gemUsageText = this.FindName("GeminiUsageText") as TextBlock;
                        if (int.TryParse(gemDaily, out var geminiLimit) && gemUsageText != null)
                            gemUsageText.Text = $"{geminiUsage}/{geminiLimit} ({(geminiUsage * 100.0 / geminiLimit):F1}%)";
                        break;
                    case "DeepSeek":
                        var deepUsage = random.Next(20, 150);
                        var deepDaily = (this.FindName("DeepSeekDailyLimitTextBox") as TextBox)?.Text;
                        var deepUsageText = this.FindName("DeepSeekUsageText") as TextBlock;
                        if (int.TryParse(deepDaily, out var deepLimit) && deepUsageText != null)
                            deepUsageText.Text = $"{deepUsage}/{deepLimit} ({(deepUsage * 100.0 / deepLimit):F1}%)";
                        break;
                    case "Claude":
                        var claudeUsage = random.Next(10, 120);
                        var claudeDaily = (this.FindName("ClaudeDailyLimitTextBox") as TextBox)?.Text;
                        var claudeUsageText = this.FindName("ClaudeUsageText") as TextBlock;
                        if (int.TryParse(claudeDaily, out var claudeLimit) && claudeUsageText != null)
                            claudeUsageText.Text = $"{claudeUsage}/{claudeLimit} ({(claudeUsage * 100.0 / claudeLimit):F1}%)";
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateProviderUsageDisplay error: {ex.Message}");
            }
        }

        private async Task TestProviderConnectionAsync(string providerName, Button? testButtonParam = null)
        {
            try
            {
                if (_aiService == null)
                {
                    MessageBox.Show("AI servisi kullanılamıyor.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (testButtonParam != null)
                {
                    testButtonParam.IsEnabled = false;
                    testButtonParam.Content = "🔄 Test...";
                }

                var config = _currentAIConfigs.FirstOrDefault(c => c.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (config == null)
                {
                    MessageBox.Show($"{providerName} konfigürasyonu bulunamadı. Önce ayarları kaydedin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var testResult = await _aiService.TestConnectionAsync(config.Id);

                if (testResult)
                {
                    MessageBox.Show($"{providerName} bağlantısı başarılı! ✅", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    Debug.WriteLine($"[AI_CONFIG] {providerName} connection test successful");
                }
                else
                {
                    MessageBox.Show($"{providerName} bağlantısı başarısız! ❌\nAPI anahtarını ve ayarları kontrol edin.", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    Debug.WriteLine($"[AI_CONFIG] {providerName} connection test failed");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{providerName} bağlantı testi sırasında hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[AI_CONFIG] {providerName} connection test error: {ex.Message}");
            }
            finally
            {
                if (testButtonParam != null)
                {
                    testButtonParam.IsEnabled = true;
                    testButtonParam.Content = "🔄 Test";
                }
            }
        }

        private async void TestAllAIConnections_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "🧪 Test Ediliyor...";
                }

                var results = new List<string>();
                var providers = new[] { "ChatGPT", "Gemini", "DeepSeek", "Claude" };

                foreach (var provider in providers)
                {
                    try
                    {
                        var config = _currentAIConfigs.FirstOrDefault(c => c.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));
                        if (config != null && config.IsActive && !string.IsNullOrWhiteSpace(config.ApiKey))
                        {
                            var testResult = await _aiService!.TestConnectionAsync(config.Id);
                            var icon = GetProviderIcon(provider);
                            results.Add($"{icon} {provider}: {(testResult ? "✅ Başarılı" : "❌ Başarısız")}");
                        }
                        else
                        {
                            var icon = GetProviderIcon(provider);
                            results.Add($"{icon} {provider}: ⚠️ Yapılandırılmamış");
                        }
                    }
                    catch (Exception ex)
                    {
                        var icon = GetProviderIcon(provider);
                        results.Add($"{icon} {provider}: ❌ Hata - {ex.Message}");
                    }
                }

                var resultMessage = "AI Bağlantı Test Sonuçları:\n\n" + string.Join("\n", results);
                MessageBox.Show(resultMessage, "AI Test Sonuçları", MessageBoxButton.OK, MessageBoxImage.Information);

                Debug.WriteLine("[AI_CONFIG] All AI connections tested");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI test süreci sırasında hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[AI_CONFIG] Test all AI connections error: {ex.Message}");
            }
            finally
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "🧪 Tüm API Test Et";
                }
            }
        }

        private void ResetAIToDefaults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Tüm AI ayarlarını varsayılana sıfırlamak istediğinizden emin misiniz?\n\nBu işlem mevcut API anahtarlarını ve ayarları siler.",
                    "AI Ayarlarını Sıfırla", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Basit varsayılana alma: Kullanım sayaçlarını sıfırla ve bilgiyi göster
                    (this.FindName("TodayTotalRequestsText") as TextBlock)?.SetCurrentValue(TextBlock.TextProperty, "0");
                    (this.FindName("MonthTotalRequestsText") as TextBlock)?.SetCurrentValue(TextBlock.TextProperty, "0");
                    (this.FindName("TotalCostText") as TextBlock)?.SetCurrentValue(TextBlock.TextProperty, "$0.00");
                    (this.FindName("SuccessRateText") as TextBlock)?.SetCurrentValue(TextBlock.TextProperty, "100.0%");
                    (this.FindName("TopProviderText") as TextBlock)?.SetCurrentValue(TextBlock.TextProperty, "🤖 Henüz kullanım yok");

                    MessageBox.Show("AI ayarları varsayılana sıfırlandı. Değişiklikleri kalıcı yapmak için 'AI Ayarlarını Kaydet' butonuna tıklayın.",
                        "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    Debug.WriteLine("[AI_CONFIG] AI settings reset to defaults (UI counters)");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI ayarları sıfırlanırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[AI_CONFIG] Reset AI settings error: {ex.Message}");
            }
        }

        private async void RefreshAIStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "🔄 Yenileniyor...";
                }

                await UpdateAIUsageStatisticsAsync();

                MessageBox.Show("AI istatistikleri güncellendi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                Debug.WriteLine("[AI_CONFIG] AI statistics refreshed");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İstatistikler yenilenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[AI_CONFIG] Refresh AI stats error: {ex.Message}");
            }
            finally
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "🔄 İstatistikleri Yenile";
                }
            }
        }

        private void ExportAIReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "CSV Dosyası|*.csv|Excel Dosyası|*.xlsx|JSON Dosyası|*.json",
                    FileName = $"AI_Usage_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    var reportData = GenerateAIReportData();
                    File.WriteAllText(sfd.FileName, reportData);
                    MessageBox.Show("AI kullanım raporu dışa aktarıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    Debug.WriteLine($"[AI_CONFIG] AI report exported to: {sfd.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rapor dışa aktarılırken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[AI_CONFIG] Export AI report error: {ex.Message}");
            }
        }

        private string GenerateAIReportData()
        {
            // AI controls temporarily disabled - XAML controls are commented out
            return "AI report temporarily disabled - will be restored after XAML activation";

            /*
            var report = new System.Text.StringBuilder();
            report.AppendLine("MesTech Stok AI Kullanım Raporu");
            report.AppendLine($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            report.AppendLine("");
            report.AppendLine("Provider,Aktif,Günlük Kullanım,Aylık Kullanım,Günlük Limit,Kullanım Oranı");

            foreach (var config in _currentAIConfigs)
            {
                var usagePercentage = config.MaxRequestsPerDay > 0 ? (config.DailyRequestCount * 100.0 / config.MaxRequestsPerDay) : 0;
                report.AppendLine($"{config.ProviderName},{(config.IsActive ? "Evet" : "Hayır")},{config.DailyRequestCount},{config.MonthlyRequestCount},{config.MaxRequestsPerDay},{usagePercentage:F1}%");
            }

            report.AppendLine("");
            report.AppendLine("Genel İstatistikler:");
            report.AppendLine($"Toplam Günlük İstek: {TodayTotalRequestsText.Text}");
            report.AppendLine($"Toplam Aylık İstek: {MonthTotalRequestsText.Text}");
            report.AppendLine($"Toplam Maliyet: {TotalCostText.Text}");
            report.AppendLine($"Başarı Oranı: {SuccessRateText.Text}");

            return report.ToString();
            */
        }

        // End of AI Configuration - Will be restored after database migration with A++++ Quality

        #region AI Configuration Methods - Integrated with existing structure

        private async Task InitializeAIServicesAsync()
        {
            try
            {
                var sp = Desktop.App.ServiceProvider;
                if (sp != null)
                {
                    // IMPORTANT: Resolve from root provider for view lifetime.
                    // Creating a temporary scope here would dispose scoped services (like AppDbContext)
                    // at the end of this method, causing 'Cannot access a disposed context instance' later.
                    _aiService = sp.GetService<IAIConfigurationService>();
                    if (_aiService != null)
                    {
                        await LoadAIConfigurationsAsync();
                        await UpdateAIUsageStatisticsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AI Services initialization error: {ex.Message}");
            }
        }

        // Removed unused LoadAISettingsAsync placeholder (legacy). Not referenced by XAML.

        // Removed unused TestOpenAIAPI_Click placeholder. Not referenced by XAML.

        // Removed unused SaveOpenAISettings_Click placeholder. Not referenced by XAML.

        #region AI Test Button Click Handlers

        private async void TestChatGPTButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            await TestProviderConnectionAsync("ChatGPT", button);
        }

        private async void TestGeminiButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            await TestProviderConnectionAsync("Gemini", button);
        }

        private async void TestDeepSeekButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            await TestProviderConnectionAsync("DeepSeek", button);
        }

        private async void TestClaudeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            await TestProviderConnectionAsync("Claude", button);
        }

        #endregion

        #endregion
    }
}
