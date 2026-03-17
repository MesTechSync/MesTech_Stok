using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Cargo tracking ViewModel — DataGrid with Takip No, Firma, Tarih, Durum, Alici.
/// 10 demo cargo entries + cargo company filter. M1 Avalonia canlandirma — Beta Agent.
/// </summary>
public partial class CargoAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedCompany = "Tumu";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<CargoItemDto> Cargos { get; } = [];

    public ObservableCollection<string> Companies { get; } =
    [
        "Tumu", "Yurtici Kargo", "Aras Kargo", "Surat Kargo", "MNG Kargo", "PTT Kargo"
    ];

    private List<CargoItemDto> _allCargos = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load

            _allCargos =
            [
                new() { TrackingNo = "YK-2026031701", Company = "Yurtici Kargo", Date = "17.03.2026", Status = "Teslim Edildi", Receiver = "Ahmet Yilmaz" },
                new() { TrackingNo = "AR-2026031702", Company = "Aras Kargo", Date = "17.03.2026", Status = "Dagitimda", Receiver = "Fatma Demir" },
                new() { TrackingNo = "SR-2026031603", Company = "Surat Kargo", Date = "16.03.2026", Status = "Transfer Merkezinde", Receiver = "Mehmet Kaya" },
                new() { TrackingNo = "YK-2026031604", Company = "Yurtici Kargo", Date = "16.03.2026", Status = "Teslim Edildi", Receiver = "Elif Celik" },
                new() { TrackingNo = "MN-2026031505", Company = "MNG Kargo", Date = "15.03.2026", Status = "Kargoya Verildi", Receiver = "Hasan Ozturk" },
                new() { TrackingNo = "PT-2026031506", Company = "PTT Kargo", Date = "15.03.2026", Status = "Dagitimda", Receiver = "Zeynep Arslan" },
                new() { TrackingNo = "AR-2026031407", Company = "Aras Kargo", Date = "14.03.2026", Status = "Teslim Edildi", Receiver = "Ali Sahin" },
                new() { TrackingNo = "YK-2026031408", Company = "Yurtici Kargo", Date = "14.03.2026", Status = "Iade Surecinde", Receiver = "Ayse Yildiz" },
                new() { TrackingNo = "SR-2026031309", Company = "Surat Kargo", Date = "13.03.2026", Status = "Teslim Edildi", Receiver = "Mustafa Erdogan" },
                new() { TrackingNo = "MN-2026031310", Company = "MNG Kargo", Date = "13.03.2026", Status = "Hasar Bildirimi", Receiver = "Selin Korkmaz" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kargo verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allCargos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.TrackingNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Receiver.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCompany != "Tumu")
        {
            filtered = filtered.Where(c => c.Company == SelectedCompany);
        }

        Cargos.Clear();
        foreach (var item in filtered)
            Cargos.Add(item);

        TotalCount = Cargos.Count;
        IsEmpty = Cargos.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allCargos.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedCompanyChanged(string value)
    {
        if (_allCargos.Count > 0)
            ApplyFilters();
    }
}

public class CargoItemDto
{
    public string TrackingNo { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
}
