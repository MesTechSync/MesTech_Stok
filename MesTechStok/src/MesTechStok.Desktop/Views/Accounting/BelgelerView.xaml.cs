using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class BelgelerView : UserControl
    {
        private readonly ObservableCollection<DocumentItem> _documents = new();

        public BelgelerView()
        {
            InitializeComponent();
            DocumentsGrid.ItemsSource = _documents;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _documents.Clear();
            _documents.Add(new DocumentItem { FileName = "fatura_trendyol_mart_2026.pdf", DocumentType = "Fatura", Source = "Trendyol", UploadDate = DateTime.Today.AddDays(-1), FileSize = "245 KB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "irsaliye_kargo_0314.pdf", DocumentType = "Irsaliye", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-2), FileSize = "128 KB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "hepsiburada_komisyon_raporu.xlsx", DocumentType = "Dekont", Source = "Hepsiburada", UploadDate = DateTime.Today.AddDays(-3), FileSize = "89 KB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "depo_kira_sozlesmesi.pdf", DocumentType = "Sozlesme", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-5), FileSize = "1.2 MB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "n11_fatura_subat.pdf", DocumentType = "Fatura", Source = "N11", UploadDate = DateTime.Today.AddDays(-7), FileSize = "312 KB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "sgk_dekont_mart.pdf", DocumentType = "Dekont", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-8), FileSize = "56 KB", Status = "Beklemede" });
            _documents.Add(new DocumentItem { FileName = "tedarikci_fatura_mega.jpg", DocumentType = "Fatura", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-9), FileSize = "780 KB", Status = "Beklemede" });
            _documents.Add(new DocumentItem { FileName = "vergi_levhasi_2026.png", DocumentType = "Diger", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-10), FileSize = "420 KB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "ciceksepeti_hesap_ozeti.pdf", DocumentType = "Dekont", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-12), FileSize = "198 KB", Status = "Islendi" });
            _documents.Add(new DocumentItem { FileName = "kargo_sozlesme_yurtici.pdf", DocumentType = "Sozlesme", Source = "Manuel", UploadDate = DateTime.Today.AddDays(-15), FileSize = "890 KB", Status = "Islendi" });
        }

        private void TypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Intentional: reload mock data — real filtering will query document store
            LoadMockData();
        }

        private void SourceFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Intentional: reload mock data — real filtering will query document store
            LoadMockData();
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Desteklenen dosyalar|*.pdf;*.jpg;*.jpeg;*.png;*.xlsx|Tum dosyalar|*.*",
                Title = "Belge Sec"
            };
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show($"Dosya secildi: {dialog.FileName}\n\nBelge yukleme islevi yakin zamanda aktif olacak.\n(Dosya depolama servisi tamamlandiginda etkinlestirilecek.)",
                    "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UploadArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    MessageBox.Show($"{files.Length} dosya suruklenildi.\n\nBelge yukleme islevi yakin zamanda aktif olacak.\n(Dosya depolama servisi tamamlandiginda etkinlestirilecek.)",
                        "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void UploadArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DocumentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DocumentsGrid.SelectedItem is DocumentItem selected)
            {
                PreviewFileName.Text = selected.FileName;
                PreviewDetails.Text = $"Tur: {selected.DocumentType}\nKaynak: {selected.Source}\nBoyut: {selected.FileSize}\nDurum: {selected.Status}\nYukleme: {selected.UploadDate:dd.MM.yyyy}";
            }
            else
            {
                PreviewFileName.Text = "Belge seciniz";
                PreviewDetails.Text = "";
            }
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class DocumentItem
    {
        public string FileName { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string Source { get; set; } = "";
        public DateTime UploadDate { get; set; }
        public string FileSize { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
