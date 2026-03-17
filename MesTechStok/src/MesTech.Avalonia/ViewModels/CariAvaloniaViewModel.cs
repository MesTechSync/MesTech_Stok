using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Cari Hesap (Account) ViewModel — DataGrid with Ad, Tip, Borc, Alacak, Bakiye.
/// 10 demo entries with Musteri/Tedarikci filter. M1 Avalonia canlandirma — Beta Agent.
/// </summary>
public partial class CariAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private int totalCount;

    public ObservableCollection<CariItemDto> Accounts { get; } = [];

    public ObservableCollection<string> AccountTypes { get; } =
    [
        "Tumu", "Musteri", "Tedarikci"
    ];

    private List<CariItemDto> _allAccounts = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load

            _allAccounts =
            [
                new() { Name = "Teknosa A.S.", Type = "Musteri", Debt = 45750.00m, Credit = 30000.00m, Balance = 15750.00m },
                new() { Name = "Samsung Turkiye", Type = "Tedarikci", Debt = 0.00m, Credit = 128300.00m, Balance = -128300.00m },
                new() { Name = "MediaMarkt Turkiye", Type = "Musteri", Debt = 67890.50m, Credit = 67890.50m, Balance = 0.00m },
                new() { Name = "Apple Turkiye Dist.", Type = "Tedarikci", Debt = 0.00m, Credit = 89120.75m, Balance = -89120.75m },
                new() { Name = "Hepsiburada Lojistik", Type = "Musteri", Debt = 23456.00m, Credit = 15000.00m, Balance = 8456.00m },
                new() { Name = "LG Elektronik", Type = "Tedarikci", Debt = 5000.00m, Credit = 34567.00m, Balance = -29567.00m },
                new() { Name = "Vatanbilgisayar Ltd.", Type = "Musteri", Debt = 15890.25m, Credit = 15890.25m, Balance = 0.00m },
                new() { Name = "Bosch Ev Aletleri", Type = "Tedarikci", Debt = 0.00m, Credit = 56780.00m, Balance = -56780.00m },
                new() { Name = "Trendyol Express", Type = "Musteri", Debt = 112340.00m, Credit = 100000.00m, Balance = 12340.00m },
                new() { Name = "Philips Turkiye", Type = "Tedarikci", Debt = 2500.00m, Credit = 41230.00m, Balance = -38730.00m },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Cari hesaplar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allAccounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
        {
            filtered = filtered.Where(a => a.Type == SelectedType);
        }

        Accounts.Clear();
        foreach (var item in filtered)
            Accounts.Add(item);

        TotalCount = Accounts.Count;
        IsEmpty = Accounts.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allAccounts.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedTypeChanged(string value)
    {
        if (_allAccounts.Count > 0)
            ApplyFilters();
    }
}

public class CariItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Debt { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}
