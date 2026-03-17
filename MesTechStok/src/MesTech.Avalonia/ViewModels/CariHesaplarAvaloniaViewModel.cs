using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cari Hesaplar (Accounts Receivable/Payable) screen.
/// Displays customer/supplier accounts with debit, credit and balance info.
/// Will be wired to GetCariAccountsPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class CariHesaplarAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private decimal totalDebit;
    [ObservableProperty] private decimal totalCredit;
    [ObservableProperty] private decimal netBalance;

    public ObservableCollection<CariHesapItemDto> Items { get; } = [];
    public ObservableCollection<CariHesapItemDto> FilteredItems { get; } = [];
    public ObservableCollection<string> TypeOptions { get; } = ["Tumu", "Musteri", "Tedarikci"];

    private readonly List<CariHesapItemDto> _allItems = [];

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
            _allItems.AddRange(new[]
            {
                new CariHesapItemDto { HesapAdi = "Anadolu Elektronik Ltd. Sti.", Tip = "Musteri", Borc = 45_250.00m, Alacak = 30_000.00m },
                new CariHesapItemDto { HesapAdi = "Yildiz Teknoloji A.S.", Tip = "Tedarikci", Borc = 12_800.00m, Alacak = 55_600.00m },
                new CariHesapItemDto { HesapAdi = "Bosphorus Ticaret Ltd. Sti.", Tip = "Musteri", Borc = 78_900.00m, Alacak = 78_900.00m },
                new CariHesapItemDto { HesapAdi = "Kuzey Bilisim A.S.", Tip = "Tedarikci", Borc = 5_400.00m, Alacak = 23_750.00m },
                new CariHesapItemDto { HesapAdi = "Marmara Lojistik Ltd. Sti.", Tip = "Musteri", Borc = 34_100.00m, Alacak = 20_000.00m },
                new CariHesapItemDto { HesapAdi = "Ege Pazarlama A.S.", Tip = "Musteri", Borc = 62_300.00m, Alacak = 45_000.00m },
                new CariHesapItemDto { HesapAdi = "Karadeniz Tedarik Ltd. Sti.", Tip = "Tedarikci", Borc = 8_900.00m, Alacak = 42_100.00m },
                new CariHesapItemDto { HesapAdi = "Istanbul Depo Hizmetleri A.S.", Tip = "Tedarikci", Borc = 15_600.00m, Alacak = 28_400.00m },
                new CariHesapItemDto { HesapAdi = "Ankara Dijital Cozumler Ltd. Sti.", Tip = "Musteri", Borc = 91_200.00m, Alacak = 60_000.00m },
                new CariHesapItemDto { HesapAdi = "Trakya Endustri A.S.", Tip = "Tedarikci", Borc = 3_200.00m, Alacak = 18_500.00m },
            });

            // Calculate bakiye for each
            foreach (var item in _allItems)
                item.Bakiye = item.Borc - item.Alacak;

            ApplyFilter();
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

    partial void OnSelectedTypeChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();

        var filtered = SelectedType == "Tumu"
            ? _allItems
            : _allItems.Where(x => x.Tip == SelectedType).ToList();

        foreach (var item in filtered)
            FilteredItems.Add(item);

        TotalCount = FilteredItems.Count;
        IsEmpty = FilteredItems.Count == 0;
        TotalDebit = FilteredItems.Sum(x => x.Borc);
        TotalCredit = FilteredItems.Sum(x => x.Alacak);
        NetBalance = TotalDebit - TotalCredit;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class CariHesapItemDto
{
    public string HesapAdi { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}
