using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Services;
using ClosedXML.Excel;

namespace MesTechStok.Desktop.Views
{
    public partial class ProductImportWizard : Window
    {
        public class MappingRow
        {
            public string FieldName { get; set; } = string.Empty;
            public bool Required { get; set; }
            public string RequiredDisplay => Required ? "Evet" : "Hayır";
            public List<string> Choices { get; set; } = new();
            public string? HeaderName { get; set; }
        }

        private readonly List<MappingRow> _mappings = new();
        private readonly List<Dictionary<string, string>> _rows = new();
        private readonly string[] _required = new[] { "Ad", "Barkod" };
        private readonly string[] _optional = new[] {
            // Ürün bilgileri
            "SKU", "Kategori",
            "Menşei", "Materyal", "Hacim", "Bedenler",
            "Boy(cm)", "En(cm)", "Yükseklik(cm)",
            "Desi", "Termin(gün)", "Sevkiyat Adresi", "İade Adresi",
            // Görseller (kapak + ekler). 'image' ya da 'Görsel' sütunu ; ile çoklu gelebilir. Ayrıca Resim 2..8 desteklenir.
            "image", "Görsel", "Kapak Görsel", "Ek Görseller", "Resim 2", "Resim 3", "Resim 4", "Resim 5", "Resim 6", "Resim 7", "Resim 8",
            // Finansal
            "Alış", "Satış", "%İskonto", "Stok", "MinStok",
            // Kategori şeması (opsiyonel – OpenCart uyumlu alanlar)
            "category_id", "parent_id", "top", "column", "sort_order", "status", "date_added", "date_modified",
            "code", "store", "link", "full_path_tr", "parent_name_tr", "filters_names_tr", "seo_keyword_tr", "name_tr", "description_tr", "meta_title_tr", "meta_description_tr", "meta_keyword_tr", "filters_ids"
        };
        private readonly IProductDataService _productService;

        public ProductImportWizard()
        {
            InitializeComponent();
            DataContext = this;
            InitMappings(Array.Empty<string>());
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                if (sp != null)
                {
                    var ctx = sp.GetService<MesTechStok.Core.Data.AppDbContext>();
                    _productService = ctx != null ? new SqlBackedProductService(ctx) : new EnhancedProductService();
                }
                else
                {
                    _productService = new EnhancedProductService();
                }
            }
            catch { _productService = new EnhancedProductService(); }
        }

        public IEnumerable<MappingRow> Mappings => _mappings;

        private void InitMappings(IEnumerable<string> headers)
        {
            _mappings.Clear();
            foreach (var f in _required)
                _mappings.Add(new MappingRow { FieldName = f, Required = true, Choices = headers.ToList(), HeaderName = headers.FirstOrDefault(h => h.Equals(f, StringComparison.OrdinalIgnoreCase)) });
            foreach (var f in _optional)
                _mappings.Add(new MappingRow { FieldName = f, Required = false, Choices = headers.ToList(), HeaderName = headers.FirstOrDefault(h => h.Equals(f, StringComparison.OrdinalIgnoreCase)) });
            MappingGrid.ItemsSource = null;
            MappingGrid.ItemsSource = _mappings;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Excel Files|*.xlsx|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                TxtFile.Text = dlg.FileName;
                _ = LoadPreviewAsync();
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadPreviewAsync();
        }

        private async Task LoadPreviewAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtFile.Text)) return;
                Busy("Dosya okunuyor…");
                var data = await Task.Run(() => ReadExcel(TxtFile.Text));
                _rows.Clear();
                _rows.AddRange(data.Rows);
                InitMappings(data.Headers);
                PreviewList.Items.Clear();
                foreach (var row in _rows.Take(10))
                {
                    PreviewList.Items.Add(string.Join(" | ", row.Select(kv => $"{kv.Key}:{kv.Value}")));
                }
                SummaryText.Text = $"Toplam satır: {data.Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Önizleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Busy();
            }
        }

        private (List<string> Headers, List<Dictionary<string, string>> Rows) ReadExcel(string path)
        {
            var headers = new List<string>();
            var rows = new List<Dictionary<string, string>>();
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.Worksheet(1);
            int col = 1;
            while (!string.IsNullOrWhiteSpace(ws.Cell(1, col).GetString()))
            {
                headers.Add(ws.Cell(1, col).GetString().Trim());
                col++;
            }
            int r = 2;
            while (!ws.Cell(r, 1).IsEmpty())
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 1; c <= headers.Count; c++)
                {
                    dict[headers[c - 1]] = ws.Cell(r, c).GetString();
                }
                rows.Add(dict);
                r++;
            }
            return (headers, rows);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Required checks
                foreach (var req in _mappings.Where(m => m.Required))
                {
                    if (string.IsNullOrWhiteSpace(req.HeaderName))
                    {
                        MessageBox.Show($"Zorunlu alan eşlenmedi: {req.FieldName}", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                Busy("İçe aktarılıyor…");
                int ok = 0, updated = 0, failed = 0;
                var errors = new StringBuilder();

                // Build a mapping dict FieldName -> HeaderName
                var map = _mappings.Where(m => !string.IsNullOrWhiteSpace(m.HeaderName)).ToDictionary(m => m.FieldName, m => m.HeaderName!, StringComparer.OrdinalIgnoreCase);

                foreach (var row in _rows)
                {
                    try
                    {
                        string Get(string key) => map.ContainsKey(key) && row.ContainsKey(map[key]) ? (row[map[key]] ?? string.Empty).Trim() : string.Empty;
                        string GetAny(params string[] keys)
                        {
                            foreach (var k in keys)
                            {
                                var v = Get(k);
                                if (!string.IsNullOrWhiteSpace(v)) return v;
                            }
                            return string.Empty;
                        }
                        var item = new Models.ProductItem
                        {
                            Name = Get("Ad"),
                            Barcode = Get("Barkod"),
                            Sku = Get("SKU"),
                            Category = Get("Kategori")
                        };
                        if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Barcode)) { failed++; continue; }

                        if (ChkValidate.IsChecked == true)
                        {
                            if (!IsValidBarcode(item.Barcode))
                            {
                                if (ChkSkipInvalid.IsChecked == true)
                                {
                                    failed++; continue;
                                }
                                else
                                {
                                    throw new Exception($"Geçersiz barkod: {item.Barcode}");
                                }
                            }
                        }
                        // Yeni alanlar
                        item.Origin = Get("Menşei");
                        item.Material = Get("Materyal");
                        item.VolumeText = Get("Hacim");
                        item.Sizes = Get("Bedenler");
                        if (decimal.TryParse(Get("Boy(cm)"), out var boy)) item.LengthCm = boy;
                        if (decimal.TryParse(Get("En(cm)"), out var en)) item.WidthCm = en;
                        if (decimal.TryParse(Get("Yükseklik(cm)"), out var yuk)) item.HeightCm = yuk;
                        if (decimal.TryParse(Get("Desi"), out var desi)) item.Desi = desi;
                        if (int.TryParse(Get("Termin(gün)"), out var lead)) item.LeadTimeDays = lead;
                        item.ShipAddress = Get("Sevkiyat Adresi");
                        item.ReturnAddress = Get("İade Adresi");

                        // Kategori gelişmiş alanlardan isim çıkarımı
                        var nameTr = Get("name_tr");
                        var fullPathTr = Get("full_path_tr");
                        if (string.IsNullOrWhiteSpace(item.Category))
                        {
                            if (!string.IsNullOrWhiteSpace(nameTr)) item.Category = nameTr;
                            else if (!string.IsNullOrWhiteSpace(fullPathTr)) item.Category = fullPathTr.Split('>').Last().Trim();
                        }

                        // Görseller (kapak + ekler). 'image' alanı ; ile çoklu gelebilir.
                        var imagesList = new List<string>();
                        var imageCombined = GetAny("image", "Görsel", "Kapak Görsel");
                        if (!string.IsNullOrWhiteSpace(imageCombined))
                        {
                            imagesList.AddRange(imageCombined.Split(new[] { ';', ',', '|', '\\', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        }
                        var extras = Get("Ek Görseller");
                        if (!string.IsNullOrWhiteSpace(extras))
                        {
                            imagesList.AddRange(extras.Split(new[] { ';', ',', '|', '\\', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
                        }
                        // Ayrı kolonlar Resim 2..8
                        for (int i = 2; i <= 8; i++)
                        {
                            var val = Get($"Resim {i}");
                            if (!string.IsNullOrWhiteSpace(val)) imagesList.Add(val.Trim());
                        }
                        // Tekilleştir ve en fazla 8 görseli kabul et
                        imagesList = imagesList.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
                        if (imagesList.Count > 0)
                        {
                            item.ImageUrl = imagesList[0];
                            if (imagesList.Count > 1)
                            {
                                item.AdditionalImageUrls = string.Join(";", imagesList.Skip(1).Take(7));
                            }
                        }

                        // Finansal
                        if (decimal.TryParse(Get("Alış"), out var alis)) item.PurchasePrice = alis;
                        if (decimal.TryParse(Get("Satış"), out var satis)) item.Price = satis;
                        if (decimal.TryParse(Get("%İskonto"), out var ind)) item.DiscountRate = ind;
                        if (int.TryParse(Get("Stok"), out var stok)) item.Stock = stok;
                        if (int.TryParse(Get("MinStok"), out var min)) item.MinimumStock = min;

                        // Service
                        var existing = await _productService.GetProductByBarcodeAsync(item.Barcode);
                        bool success;
                        if (existing != null)
                        {
                            item.Id = existing.Id;
                            success = await _productService.UpdateProductAsync(item);
                            if (success) updated++;
                        }
                        else
                        {
                            success = await _productService.AddProductAsync(item);
                            if (success) ok++;
                        }
                        // Başarılıysa görsel sayısı etiketi (istatistik için)
                        if (success)
                        {
                            var imgCount = 0;
                            if (!string.IsNullOrWhiteSpace(item.ImageUrl)) imgCount++;
                            if (!string.IsNullOrWhiteSpace(item.AdditionalImageUrls))
                                imgCount += item.AdditionalImageUrls.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
                            // Opsiyonel: 8 üstü kırpıldı bilgisini özetleyelim
                            if (imgCount > 8) imgCount = 8;
                        }
                        if (!success)
                        {
                            failed++;
                            errors.AppendLine($"{item.Barcode} kaydedilemedi");
                        }
                    }
                    catch (Exception ex2)
                    {
                        failed++;
                        errors.AppendLine(ex2.Message);
                    }
                }

                SummaryText.Text = $"Eklendi={ok}, Güncellendi={updated}, Hata={failed}";
                BtnExportErrors.Visibility = failed > 0 ? Visibility.Visible : Visibility.Collapsed;
                if (failed == 0) DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İçe aktarma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Busy();
            }
        }

        // no owner-callbacks; ProductsView kapanışta kendisi yenileyebilir

        private static bool IsValidBarcode(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = new string(s.Where(char.IsDigit).ToArray());
            // Allow EAN-13, EAN-8, UPC-A(12), UPC-E(8) basic length check + checksum for EAN-13/8
            if (s.Length == 13) return EanChecksumValid(s);
            if (s.Length == 8) return Ean8ChecksumValid(s);
            if (s.Length == 12) return true; // UPC-A: opsiyonel checksum kontrolü eklenebilir
            return false;
        }

        private static bool EanChecksumValid(string code)
        {
            if (code.Length != 13 || !code.All(char.IsDigit)) return false;
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int n = code[i] - '0';
                sum += (i % 2 == 0) ? n : n * 3;
            }
            int check = (10 - (sum % 10)) % 10;
            return check == (code[12] - '0');
        }

        private static bool Ean8ChecksumValid(string code)
        {
            if (code.Length != 8 || !code.All(char.IsDigit)) return false;
            int sum = 0;
            for (int i = 0; i < 7; i++)
            {
                int n = code[i] - '0';
                sum += (i % 2 == 0) ? n * 3 : n;
            }
            int check = (10 - (sum % 10)) % 10;
            return check == (code[7] - '0');
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = _mappings.ToDictionary(m => m.FieldName, m => m.HeaderName ?? string.Empty);
                var sfd = new SaveFileDialog { Filter = "JSON|*.json", FileName = "import-profile.json" };
                if (sfd.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(sfd.FileName, json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Profil kaydetme hatası: {ex.Message}", "Hata");
            }
        }

        private void LoadProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog { Filter = "JSON|*.json" };
                if (ofd.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(ofd.FileName);
                    var profile = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                    foreach (var m in _mappings)
                    {
                        if (profile.TryGetValue(m.FieldName, out var header)) m.HeaderName = header;
                    }
                    MappingGrid.ItemsSource = null; MappingGrid.ItemsSource = _mappings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Profil yükleme hatası: {ex.Message}", "Hata");
            }
        }

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "urun_sablon.xlsx" };
                if (sfd.ShowDialog() == true)
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.AddWorksheet("Sablon");
                    string[] headers = new[] {
                        // Zorunlu
                        "Ad", "Barkod",
                        // Önerilen
                        "SKU", "Kategori", "Menşei", "Materyal", "Hacim", "Bedenler",
                        "Boy(cm)", "En(cm)", "Yükseklik(cm)",
                        "Desi", "Termin(gün)", "Sevkiyat Adresi", "İade Adresi",
                        // Görseller
                        "image", // Birden fazla yol ; ile ayrılabilir (kapak ilk)
                        "Ek Görseller", // Alternatif: ikinci/üçüncü ...
                        "Resim 2", "Resim 3", "Resim 4", "Resim 5", "Resim 6", "Resim 7", "Resim 8",
                        // Finansal
                        "Alış", "Satış", "%İskonto", "Stok", "MinStok",
                        // OpenCart/Tedarikçi uyumlu opsiyonel kategori alanları
                        "category_id", "parent_id", "top", "column", "sort_order", "status", "date_added", "date_modified",
                        "code", "store", "link", "full_path_tr", "parent_name_tr", "filters_names_tr", "seo_keyword_tr", "name_tr", "description_tr", "meta_title_tr", "meta_description_tr", "meta_keyword_tr", "filters_ids"
                    };
                    for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
                    ws.Row(1).Style.Font.Bold = true;
                    // örnek satır
                    ws.Cell(2, 1).Value = "Deneme Ürün";
                    ws.Cell(2, 2).Value = "8691234567890";
                    ws.Cell(2, 3).Value = "SKU-001";
                    ws.Cell(2, 4).Value = "Genel";
                    ws.Cell(2, 5).Value = "TR";
                    ws.Cell(2, 6).Value = "Metal";
                    ws.Cell(2, 7).Value = "300 ml";
                    ws.Cell(2, 8).Value = "S,M,L";
                    ws.Cell(2, 9).Value = 10; // Boy
                    ws.Cell(2, 10).Value = 8; // En
                    ws.Cell(2, 11).Value = 5; // Yükseklik
                    ws.Cell(2, 12).Value = 0.5; // Desi
                    ws.Cell(2, 13).Value = 3; // Termin
                    ws.Cell(2, 14).Value = "Depo-1";
                    ws.Cell(2, 15).Value = "Depo-1";
                    ws.Cell(2, 16).Value = "C:\\resimler\\urun1.jpg;C:\\resimler\\urun1-1.jpg"; // Kapak+ek ; ile ayrık
                    ws.Cell(2, 17).Value = "C:\\resimler\\urun1-2.jpg"; // Ek Görseller
                    ws.Cell(2, 18).Value = "C:\\resimler\\urun1-3.jpg"; // Resim 2
                    ws.Cell(2, 19).Value = "C:\\resimler\\urun1-4.jpg"; // Resim 3
                    ws.Cell(2, 20).Value = "C:\\resimler\\urun1-5.jpg"; // Resim 4
                    ws.Cell(2, 21).Value = "C:\\resimler\\urun1-6.jpg"; // Resim 5
                    ws.Cell(2, 22).Value = "C:\\resimler\\urun1-7.jpg"; // Resim 6
                    ws.Cell(2, 23).Value = "C:\\resimler\\urun1-8.jpg"; // Resim 7
                    ws.Cell(2, 24).Value = "C:\\resimler\\urun1-9.jpg"; // Resim 8
                    ws.Cell(2, 25).Value = 50; // Alış
                    ws.Cell(2, 26).Value = 75; // Satış
                    ws.Cell(2, 27).Value = 5;  // %İskonto
                    ws.Cell(2, 28).Value = 12; // Stok
                    ws.Cell(2, 29).Value = 3;  // MinStok
                    ws.Columns().AdjustToContents();
                    wb.SaveAs(sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şablon indirme hatası: {ex.Message}", "Hata");
            }
        }

        private void ExportErrors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog { Filter = "Text|*.txt", FileName = $"ImportErrors_{DateTime.Now:yyyyMMdd_HHmmss}.txt" };
                if (sfd.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(sfd.FileName, SummaryText.Text);
                }
            }
            catch { }
        }

        private void Busy(string? text = null)
        {
            BusyOverlay.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            BusyText.Text = text ?? string.Empty;
        }
    }
}


