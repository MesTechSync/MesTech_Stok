using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class ProductUploadPopup_Modern : UserControl
    {
        private string? _selectedFilePath;

        public ProductUploadPopup_Modern()
        {
            InitializeComponent();
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Urun Dosyasi Secin",
                    Filter = "Excel Dosyalari (*.xlsx)|*.xlsx|CSV Dosyalari (*.csv)|*.csv|XML Dosyalari (*.xml)|*.xml|Tum Dosyalar (*.*)|*.*",
                    FilterIndex = 1
                };

                if (dlg.ShowDialog() == true)
                {
                    SetSelectedFile(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Dosya secilirken hata: {ex.Message}");
            }
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void DropZone_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0)
                    {
                        var ext = Path.GetExtension(files[0]).ToLowerInvariant();
                        if (ext is ".xlsx" or ".csv" or ".xml")
                        {
                            SetSelectedFile(files[0]);
                        }
                        else
                        {
                            ShowError("Desteklenmeyen dosya formati. Lutfen .xlsx, .csv veya .xml dosyasi secin.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Dosya suruklerken hata: {ex.Message}");
            }
        }

        private void SetSelectedFile(string filePath)
        {
            _selectedFilePath = filePath;
            TxtFileName.Text = Path.GetFileName(filePath);
            var fileInfo = new FileInfo(filePath);
            TxtFileSize.Text = fileInfo.Length < 1024 * 1024
                ? $"{fileInfo.Length / 1024.0:N1} KB"
                : $"{fileInfo.Length / (1024.0 * 1024.0):N1} MB";
            FileInfoPanel.Visibility = Visibility.Visible;
            BtnUpload.IsEnabled = true;
            ResultPanel.Visibility = Visibility.Collapsed;
            ShowContent();
        }

        private void BtnClearFile_Click(object sender, RoutedEventArgs e)
        {
            _selectedFilePath = null;
            FileInfoPanel.Visibility = Visibility.Collapsed;
            BtnUpload.IsEnabled = false;
            ResultPanel.Visibility = Visibility.Collapsed;
        }

        private async void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
            {
                ShowError("Lutfen gecerli bir dosya secin.");
                return;
            }

            try
            {
                BtnUpload.IsEnabled = false;
                BtnSelectFile.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;
                ResultPanel.Visibility = Visibility.Collapsed;

                // Simulate upload progress (replace with real API call)
                for (int i = 0; i <= 100; i += 5)
                {
                    UploadProgress.Value = i;
                    TxtProgressPercent.Text = $"{i}%";
                    TxtProgressLabel.Text = i < 50 ? "Dosya okunuyor..." : "Urunler isleniyor...";
                    await Task.Delay(50);
                }

                TxtProgressLabel.Text = "Tamamlandi";

                // Show success result
                ProgressPanel.Visibility = Visibility.Collapsed;
                ResultPanel.Visibility = Visibility.Visible;
                TxtResultTitle.Text = "Yukleme Tamamlandi";
                TxtResultDetail.Text = $"Dosya: {Path.GetFileName(_selectedFilePath)} basariyla yuklendi.";
            }
            catch (Exception ex)
            {
                ProgressPanel.Visibility = Visibility.Collapsed;
                ShowError($"Yukleme hatasi: {ex.Message}");
            }
            finally
            {
                BtnUpload.IsEnabled = true;
                BtnSelectFile.IsEnabled = true;
            }
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
