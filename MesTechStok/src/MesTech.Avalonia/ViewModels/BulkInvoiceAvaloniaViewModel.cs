using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Toplu fatura olusturma ViewModel.
/// Faturalanmamis siparisleri secip toplu e-Fatura uretimi.
/// </summary>
public partial class BulkInvoiceAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private bool isProcessing;
    [ObservableProperty] private double progress;
    [ObservableProperty] private int selectedCount;
    [ObservableProperty] private int successCount;
    [ObservableProperty] private int failCount;
    [ObservableProperty] private bool selectAll;
    [ObservableProperty] private string selectedProvider = "Sovos";
    [ObservableProperty] private bool showResults;

    public ObservableCollection<BulkInvoiceOrderDto> Orders { get; } = [];
    public ObservableCollection<string> ErrorDetails { get; } = [];

    public ObservableCollection<string> Providers { get; } =
    [
        "Sovos", "GIB Portal", "Foriba", "Logo e-Fatura"
    ];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ShowResults = false;
        try
        {
            await Task.Delay(300);

            Orders.Clear();
            Orders.Add(new() { OrderId = "SIP-2026-0501", CustomerName = "Yilmaz Elektronik Ltd. Sti.", Amount = 12450.00m, Date = new DateTime(2026, 3, 17), Platform = "Trendyol" });
            Orders.Add(new() { OrderId = "SIP-2026-0502", CustomerName = "Demir Bilisim A.S.", Amount = 8320.50m, Date = new DateTime(2026, 3, 16), Platform = "Hepsiburada" });
            Orders.Add(new() { OrderId = "SIP-2026-0503", CustomerName = "Kaya Ticaret ve Sanayi", Amount = 3250.00m, Date = new DateTime(2026, 3, 15), Platform = "N11" });
            Orders.Add(new() { OrderId = "SIP-2026-0504", CustomerName = "Arslan Mobilya", Amount = 15400.75m, Date = new DateTime(2026, 3, 14), Platform = "Trendyol" });
            Orders.Add(new() { OrderId = "SIP-2026-0505", CustomerName = "Celik Otomotiv San.", Amount = 27200.00m, Date = new DateTime(2026, 3, 13), Platform = "Amazon" });
            Orders.Add(new() { OrderId = "SIP-2026-0506", CustomerName = "Ozturk Gida Paz.", Amount = 4980.25m, Date = new DateTime(2026, 3, 12), Platform = "Ciceksepeti" });
            Orders.Add(new() { OrderId = "SIP-2026-0507", CustomerName = "Sahin Insaat Malz.", Amount = 52300.00m, Date = new DateTime(2026, 3, 11), Platform = "Trendyol" });

            UpdateSelectedCount();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Siparisler yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnSelectAllChanged(bool value)
    {
        foreach (var order in Orders)
            order.IsSelected = value;
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Orders.Count(o => o.IsSelected);
    }

    [RelayCommand]
    private async Task ProcessBulkAsync()
    {
        var selected = Orders.Where(o => o.IsSelected).ToList();
        if (selected.Count == 0) return;

        IsProcessing = true;
        ShowResults = false;
        SuccessCount = 0;
        FailCount = 0;
        Progress = 0;
        ErrorDetails.Clear();

        for (int i = 0; i < selected.Count; i++)
        {
            await Task.Delay(400); // Simulate per-invoice processing
            Progress = (double)(i + 1) / selected.Count * 100;

            // Simulate: ~80% success, ~20% fail
            if (i % 5 == 3)
            {
                FailCount++;
                ErrorDetails.Add($"{selected[i].OrderId}: GIB baglanti hatasi — zaman asimi");
            }
            else
            {
                SuccessCount++;
            }
        }

        IsProcessing = false;
        ShowResults = true;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class BulkInvoiceOrderDto : ObservableObject
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Platform { get; set; } = string.Empty;

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
