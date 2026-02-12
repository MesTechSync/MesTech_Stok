using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using WinForms = System.Windows.Forms;

namespace MesTechStok.Desktop.Views
{
    public partial class ImageMapWizard : Window
    {
        private readonly List<Row> _rows = new();
        private readonly string _corr = $"IMAGE_MAP-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        private class Row
        {
            public string FileName { get; set; } = string.Empty;
            public string MatchedBy { get; set; } = string.Empty;
            public string Barcode { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string FullPath { get; set; } = string.Empty;
        }

        public ImageMapWizard()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WinForms.FolderBrowserDialog { Description = "Görsel klasörünü seçin" };
            if (dlg.ShowDialog() == WinForms.DialogResult.OK)
            {
                TxtFolder.Text = dlg.SelectedPath;
            }
        }

        private async void Scan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtFolder.Text) || !Directory.Exists(TxtFolder.Text)) { MessageBox.Show("Klasör seçin"); return; }
                Busy("Taranıyor…");
                _rows.Clear();
                var files = Directory.EnumerateFiles(TxtFolder.Text).Where(f => HasImageExt(f)).ToList();
                await Task.Run(() => BuildPreview(files));
                PreviewGrid.ItemsSource = null;
                PreviewGrid.ItemsSource = _rows;
                ApplyFilters();
                SummaryText.Text = $"Bulunan: {files.Count}, Eşleşen: {_rows.Count(r => r.ProductId > 0)}";
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("IMPORT", $"[IMAGE_MAP] PREVIEW corr={_corr} files={files.Count}", nameof(ImageMapWizard)); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Önizleme hatası: {ex.Message}");
            }
            finally { Busy(); }
        }

        private void FilterToggles_Changed(object sender, RoutedEventArgs e)
        {
            try { ApplyFilters(); } catch { }
        }

        private void ApplyFilters()
        {
            try
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(PreviewGrid.ItemsSource);
                if (view == null) return;
                bool onlyMatched = ChkOnlyMatched.IsChecked == true;
                bool onlyUnmatched = ChkOnlyUnmatched.IsChecked == true;
                if (onlyMatched && onlyUnmatched)
                {
                    // ikisi birden seçiliyse filtreleme yapma
                    view.Filter = null;
                }
                else if (!onlyMatched && !onlyUnmatched)
                {
                    view.Filter = null;
                }
                else if (onlyMatched)
                {
                    view.Filter = o => o is Row r && r.ProductId > 0;
                }
                else // onlyUnmatched
                {
                    view.Filter = o => o is Row r && r.ProductId <= 0;
                }
                view.Refresh();
            }
            catch { }
        }

        private void BuildPreview(List<string> files)
        {
            try
            {
                var sp = App.ServiceProvider;
                if (sp == null) return;
                var ctx = sp.GetService(typeof(MesTechStok.Core.Data.AppDbContext)) as MesTechStok.Core.Data.AppDbContext;
                if (ctx == null) return;
                foreach (var f in files)
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    var row = new Row { FileName = Path.GetFileName(f), FullPath = f };
                    if (ChkMatchBarcode.IsChecked == true)
                    {
                        var prod = ctx.Products.FirstOrDefault(p => p.Barcode == name || (ChkContains.IsChecked == true && name.Contains(p.Barcode)));
                        if (prod != null)
                        {
                            row.ProductId = prod.Id; row.Barcode = prod.Barcode; row.ProductName = prod.Name; row.MatchedBy = "Barkod"; _rows.Add(row); continue;
                        }
                    }
                    if (ChkMatchSku.IsChecked == true)
                    {
                        var prod = ctx.Products.FirstOrDefault(p => p.SKU == name || (ChkContains.IsChecked == true && name.Contains(p.SKU)));
                        if (prod != null)
                        {
                            row.ProductId = prod.Id; row.Sku = prod.SKU; row.ProductName = prod.Name; row.MatchedBy = "SKU"; _rows.Add(row); continue;
                        }
                    }
                    row.Status = "Eşleşmedi"; _rows.Add(row);
                }
            }
            catch { }
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rows.Count == 0) return;
                Busy("Eşleştiriliyor…");
                var sp = App.ServiceProvider; if (sp == null) return;
                var ctx = sp.GetService(typeof(MesTechStok.Core.Data.AppDbContext)) as MesTechStok.Core.Data.AppDbContext; if (ctx == null) return;
                var storage = new MesTechStok.Desktop.Services.ImageStorageService();
                int ok = 0, fail = 0;
                foreach (var r in _rows.Where(r => r.ProductId > 0))
                {
                    try
                    {
                        var res = await storage.SaveAsync(r.ProductId, r.FullPath);
                        var p = ctx.Products.FirstOrDefault(x => x.Id == r.ProductId);
                        if (p != null && !string.IsNullOrWhiteSpace(res.Full1200))
                        {
                            p.ImageUrl = res.Full1200; await ctx.SaveChangesAsync(); ok++; r.Status = "Güncellendi";
                            try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"ImageUpdated Id={p.Id} File={System.IO.Path.GetFileName(r.FullPath)}", nameof(ImageMapWizard)); } catch { }
                        }
                    }
                    catch { fail++; r.Status = "Hata"; }
                }
                PreviewGrid.Items.Refresh();
                SummaryText.Text = $"Eşleşen: {_rows.Count(r => r.ProductId > 0)} · Güncellendi={ok} · Hata={fail}";
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("IMPORT", $"[IMAGE_MAP] APPLY corr={_corr} ok={ok} fail={fail}", nameof(ImageMapWizard)); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eşleme hatası: {ex.Message}");
            }
            finally { Busy(); }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rows.Count == 0) return;
                var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "CSV|*.csv", FileName = $"ImageMap_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
                if (sfd.ShowDialog() == true)
                {
                    using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine("FileName,MatchedBy,Barcode,Sku,ProductName,Status");
                    var view = System.Windows.Data.CollectionViewSource.GetDefaultView(PreviewGrid.ItemsSource);
                    IEnumerable<Row> source = _rows;
                    if (view != null && view.Filter != null)
                    {
                        source = _rows.Where(r => (view.Filter?.Invoke(r) ?? true));
                    }
                    foreach (var r in source)
                    {
                        string Esc(string s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";
                        sw.WriteLine(string.Join(",", new[] { Esc(r.FileName), Esc(r.MatchedBy), Esc(r.Barcode), Esc(r.Sku), Esc(r.ProductName), Esc(r.Status) }));
                    }
                }
            }
            catch { }
        }

        private bool HasImageExt(string f)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".webp";
        }

        private void Busy(string? text = null)
        {
            BusyOverlay.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            BusyText.Text = text ?? string.Empty;
        }
    }
}


