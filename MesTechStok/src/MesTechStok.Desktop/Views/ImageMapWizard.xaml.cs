using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinForms = System.Windows.Forms;

namespace MesTechStok.Desktop.Views
{
    public partial class ImageMapWizard : Window
    {
        private readonly ILogger<ImageMapWizard>? _logger;
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
            public Guid ProductId { get; set; }
            public string FullPath { get; set; } = string.Empty;
        }

        public ImageMapWizard()
        {
            InitializeComponent();
            _logger = MesTechStok.Desktop.App.Services?.GetService<ILogger<ImageMapWizard>>();
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
                SummaryText.Text = $"Bulunan: {files.Count}, Eşleşen: {_rows.Count(r => r.ProductId != Guid.Empty)}";
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("IMPORT", $"[IMAGE_MAP] PREVIEW corr={_corr} files={files.Count}", nameof(ImageMapWizard));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Önizleme hatası: {ex.Message}");
            }
            finally { Busy(); }
        }

        private void FilterToggles_Changed(object sender, RoutedEventArgs e)
        {
            try { ApplyFilters(); }
            catch (Exception ex) { /* Intentional: filter toggle event handler — must not crash on early UI access before ItemsSource is set. */ _logger?.LogWarning(ex, "{ViewName} - {Context}: {Message}", nameof(ImageMapWizard), "Filter toggle — early UI access before ItemsSource is set", ex.Message); }
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
                    view.Filter = o => o is Row r && r.ProductId != Guid.Empty;
                }
                else // onlyUnmatched
                {
                    view.Filter = o => o is Row r && r.ProductId == Guid.Empty;
                }
                view.Refresh();
            }
            catch
            {
                // Intentional: CollectionView filter apply — ItemsSource may not be set yet.
            }
        }

        private void BuildPreview(List<string> files)
        {
            try
            {
                var sp = App.Services;
                if (sp == null) return;
                using var scope = sp.CreateScope();
                var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>();
                if (mediator == null) return;

                // Fetch all products via CQRS query (replaces legacy AppDbContext direct access)
                var products = mediator.Send(
                    new MesTech.Application.Queries.SearchProductsForImageMatch.SearchProductsForImageMatchQuery())
                    .GetAwaiter().GetResult();
                if (products == null || products.Count == 0) return;

                bool matchBarcode = false, matchSku = false, useContains = false;
                Dispatcher.Invoke(() =>
                {
                    matchBarcode = ChkMatchBarcode.IsChecked == true;
                    matchSku = ChkMatchSku.IsChecked == true;
                    useContains = ChkContains.IsChecked == true;
                });

                foreach (var f in files)
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    var row = new Row { FileName = Path.GetFileName(f), FullPath = f };
                    if (matchBarcode)
                    {
                        var prod = products.FirstOrDefault(p =>
                            !string.IsNullOrEmpty(p.Barcode) &&
                            (p.Barcode == name || (useContains && name.Contains(p.Barcode))));
                        if (prod != null)
                        {
                            row.ProductId = prod.Id; row.Barcode = prod.Barcode ?? string.Empty; row.ProductName = prod.Name; row.MatchedBy = "Barkod"; _rows.Add(row); continue;
                        }
                    }
                    if (matchSku)
                    {
                        var prod = products.FirstOrDefault(p =>
                            !string.IsNullOrEmpty(p.SKU) &&
                            (p.SKU == name || (useContains && name.Contains(p.SKU))));
                        if (prod != null)
                        {
                            row.ProductId = prod.Id; row.Sku = prod.SKU; row.ProductName = prod.Name; row.MatchedBy = "SKU"; _rows.Add(row); continue;
                        }
                    }
                    row.Status = "Eşleşmedi"; _rows.Add(row);
                }
            }
            catch
            {
                // Intentional: image-product match preview build — DB may be unavailable on first open.
            }
        }

        private async void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rows.Count == 0) return;
                Busy("Eşleştiriliyor…");
                var sp = App.Services; if (sp == null) return;
                using var scope = sp.CreateScope();
                var mediator = scope.ServiceProvider.GetService<MediatR.IMediator>(); if (mediator == null) return;
                var storage = new MesTechStok.Desktop.Services.ImageStorageService();
                int ok = 0, fail = 0;
                foreach (var r in _rows.Where(r => r.ProductId != Guid.Empty))
                {
                    try
                    {
                        var res = await storage.SaveAsync(r.ProductId, r.FullPath);
                        if (!string.IsNullOrWhiteSpace(res.Full1200))
                        {
                            var cmd = new MesTech.Application.Commands.UpdateProductImage.UpdateProductImageCommand(r.ProductId, res.Full1200);
                            var result = await mediator.Send(cmd);
                            if (result.IsSuccess) { ok++; r.Status = "Güncellendi"; }
                            else { fail++; r.Status = "Hata"; }
                            MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("PRODUCT_AUDIT", $"ImageUpdated Id={r.ProductId} File={System.IO.Path.GetFileName(r.FullPath)}", nameof(ImageMapWizard));
                        }
                    }
                    catch (Exception ex) { /* Intentional: image upload error tracking — count failure and continue batch */ _logger?.LogWarning(ex, "{ViewName} - {Context}: {Message}", nameof(ImageMapWizard), "Image upload batch — count failure and continue", ex.Message); fail++; r.Status = "Hata"; }
                }
                PreviewGrid.Items.Refresh();
                SummaryText.Text = $"Eşleşen: {_rows.Count(r => r.ProductId != Guid.Empty)} · Güncellendi={ok} · Hata={fail}";
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("IMPORT", $"[IMAGE_MAP] APPLY corr={_corr} ok={ok} fail={fail}", nameof(ImageMapWizard));
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
            catch
            {
                // Intentional: CSV export click event handler — file write may fail if path is read-only.
            }
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


