using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace MesTech.Avalonia.Dialogs;

public partial class ImageDialog : Window
{
    private readonly List<string> _imagePaths = new();
    public IReadOnlyList<string> ImagePaths => _imagePaths;

    public ImageDialog() : this("Resim Galerisi") { }

    public ImageDialog(string title = "Resim Galerisi")
    {
        InitializeComponent();
        TitleText.Text = title;
    }

    private async void OnAddImage(object? sender, RoutedEventArgs e)
    {
        try
        {
            var storage = StorageProvider;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Resim Sec",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Resim Dosyalari") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp" } }
                }
            });

            foreach (var file in files)
            {
                _imagePaths.Add(file.Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageDialog] Resim secme hatasi: {ex.Message}");
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
