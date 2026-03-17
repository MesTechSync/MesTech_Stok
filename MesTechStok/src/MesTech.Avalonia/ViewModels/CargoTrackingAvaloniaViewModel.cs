using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cargo Tracking screen — Dalga 14/15.
/// Displays cargo shipments with firm-based filtering.
/// </summary>
public partial class CargoTrackingAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedFirm = "Tümü";

    private readonly List<CargoTrackingItemDto> _allItems = [];

    public ObservableCollection<CargoTrackingItemDto> Shipments { get; } = [];

    public ObservableCollection<string> Firms { get; } =
    [
        "Tümü",
        "Yurtiçi Kargo",
        "Aras Kargo",
        "Sürat Kargo"
    ];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(80); // Simulate async load

            _allItems.Clear();
            _allItems.AddRange(
            [
                new CargoTrackingItemDto { TakipNo = "YK-2026031701", Firma = "Yurtiçi Kargo", Tarih = new DateTime(2026, 3, 15), Durum = "Teslim Edildi", Alici = "Ahmet Yılmaz" },
                new CargoTrackingItemDto { TakipNo = "AR-2026031702", Firma = "Aras Kargo", Tarih = new DateTime(2026, 3, 15), Durum = "Yolda", Alici = "Mehmet Demir" },
                new CargoTrackingItemDto { TakipNo = "SK-2026031703", Firma = "Sürat Kargo", Tarih = new DateTime(2026, 3, 14), Durum = "Dagitimda", Alici = "Fatma Kaya" },
                new CargoTrackingItemDto { TakipNo = "YK-2026031704", Firma = "Yurtiçi Kargo", Tarih = new DateTime(2026, 3, 14), Durum = "Teslim Edildi", Alici = "Ali Çelik" },
                new CargoTrackingItemDto { TakipNo = "AR-2026031705", Firma = "Aras Kargo", Tarih = new DateTime(2026, 3, 13), Durum = "Hazirlaniyor", Alici = "Zeynep Arslan" },
                new CargoTrackingItemDto { TakipNo = "SK-2026031706", Firma = "Sürat Kargo", Tarih = new DateTime(2026, 3, 13), Durum = "Teslim Edildi", Alici = "Hasan Öztürk" },
                new CargoTrackingItemDto { TakipNo = "YK-2026031707", Firma = "Yurtiçi Kargo", Tarih = new DateTime(2026, 3, 12), Durum = "Yolda", Alici = "Ayşe Şahin" },
                new CargoTrackingItemDto { TakipNo = "AR-2026031708", Firma = "Aras Kargo", Tarih = new DateTime(2026, 3, 12), Durum = "Teslim Edildi", Alici = "Mustafa Koç" },
                new CargoTrackingItemDto { TakipNo = "SK-2026031709", Firma = "Sürat Kargo", Tarih = new DateTime(2026, 3, 11), Durum = "Dagitimda", Alici = "Emine Yıldız" },
                new CargoTrackingItemDto { TakipNo = "YK-2026031710", Firma = "Yurtiçi Kargo", Tarih = new DateTime(2026, 3, 11), Durum = "Hazirlaniyor", Alici = "İbrahim Aydın" },
            ]);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kargo verileri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnSelectedFirmChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Shipments.Clear();
        var filtered = SelectedFirm == "Tümü"
            ? _allItems
            : _allItems.Where(s => s.Firma == SelectedFirm).ToList();

        foreach (var item in filtered)
            Shipments.Add(item);

        TotalCount = Shipments.Count;
        IsEmpty = Shipments.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class CargoTrackingItemDto
{
    public string TakipNo { get; set; } = string.Empty;
    public string Firma { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string Alici { get; set; } = string.Empty;
}
