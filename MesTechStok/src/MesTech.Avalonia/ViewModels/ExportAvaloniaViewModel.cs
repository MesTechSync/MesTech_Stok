using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dışa Aktar ViewModel — WPF011.
/// Format seçimi + checkbox seçimi + progress simülasyonu.
/// </summary>
public partial class ExportAvaloniaViewModel : ViewModelBase
{
    // Data type checkboxes
    [ObservableProperty] private bool exportProducts = true;
    [ObservableProperty] private bool exportOrders = true;
    [ObservableProperty] private bool exportStock;
    [ObservableProperty] private bool exportCustomers;
    [ObservableProperty] private bool exportInvoices;

    // Format selection
    [ObservableProperty] private string selectedFormat = "Excel (.xlsx)";

    // Progress
    [ObservableProperty] private int exportProgress;
    [ObservableProperty] private bool isExporting;
    [ObservableProperty] private string exportMessage = string.Empty;

    public ObservableCollection<string> Formats { get; } = new()
    {
        "Excel (.xlsx)",
        "CSV",
        "JSON"
    };

    public override Task LoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private async Task ExportAsync()
    {
        var selected = new List<string>();
        if (ExportProducts) selected.Add("Ürünler");
        if (ExportOrders) selected.Add("Siparişler");
        if (ExportStock) selected.Add("Stok");
        if (ExportCustomers) selected.Add("Müşteriler");
        if (ExportInvoices) selected.Add("Faturalar");

        if (selected.Count == 0)
        {
            ExportMessage = "Hata: En az bir veri türü seçmelisiniz.";
            return;
        }

        IsExporting = true;
        ExportProgress = 0;
        ExportMessage = string.Empty;

        try
        {
            var stepCount = selected.Count;
            for (int i = 0; i < stepCount; i++)
            {
                ExportMessage = $"{selected[i]} dışa aktarılıyor...";
                int baseProgress = (i * 100) / stepCount;
                int nextProgress = ((i + 1) * 100) / stepCount;

                // Simulate inner progress for each data type
                for (int p = baseProgress; p <= nextProgress; p += 5)
                {
                    await Task.Delay(80, CancellationToken);
                    ExportProgress = p;
                }
            }

            ExportProgress = 100;
            ExportMessage = $"{selected.Count} veri türü başarıyla {SelectedFormat} formatında dışa aktarıldı.";
        }
        catch (OperationCanceledException)
        {
            ExportMessage = "Dışa aktarma iptal edildi.";
        }
        catch (Exception ex)
        {
            ExportMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }
}
