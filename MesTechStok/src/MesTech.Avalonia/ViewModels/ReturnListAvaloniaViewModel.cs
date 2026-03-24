using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Return List screen — I-05 Siparis/Kargo Celiklestirme.
/// Displays return requests with status filtering and search.
/// </summary>
public partial class ReturnListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<ReturnListItemDto> _allItems = [];

    public ObservableCollection<ReturnListItemDto> Returns { get; } = [];

    public ObservableCollection<string> StatusFilters { get; } =
    [
        "Tumu", "Beklemede", "Onaylandi", "Reddedildi", "Yolda", "Teslim Alindi", "Iade Edildi", "Iptal"
    ];

    public ReturnListAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(150);

            _allItems.Clear();
            _allItems.AddRange(
            [
                new() { IadeNo = "IAD-2001", SiparisNo = "SIP-1001", Musteri = "Ahmet Yilmaz", Platform = "Trendyol", Tutar = 249.90m, Sebep = "Hatali Urun", Durum = "Beklemede", Tarih = new DateTime(2026, 3, 18) },
                new() { IadeNo = "IAD-2002", SiparisNo = "SIP-1005", Musteri = "Ali Ozturk", Platform = "Hepsiburada", Tutar = 129.00m, Sebep = "Yanlis Beden", Durum = "Onaylandi", Tarih = new DateTime(2026, 3, 17) },
                new() { IadeNo = "IAD-2003", SiparisNo = "SIP-1008", Musteri = "Fatma Sahin", Platform = "N11", Tutar = 89.50m, Sebep = "Musteri Vazgecmesi", Durum = "Reddedildi", Tarih = new DateTime(2026, 3, 16) },
                new() { IadeNo = "IAD-2004", SiparisNo = "SIP-1012", Musteri = "Mehmet Kaya", Platform = "Trendyol", Tutar = 449.00m, Sebep = "Kargoda Hasar", Durum = "Yolda", Tarih = new DateTime(2026, 3, 15) },
                new() { IadeNo = "IAD-2005", SiparisNo = "SIP-1015", Musteri = "Zeynep Arslan", Platform = "Ciceksepeti", Tutar = 199.90m, Sebep = "Yanlis Renk", Durum = "Teslim Alindi", Tarih = new DateTime(2026, 3, 14) },
                new() { IadeNo = "IAD-2006", SiparisNo = "SIP-1020", Musteri = "Hasan Ozturk", Platform = "Pazarama", Tutar = 75.00m, Sebep = "Aciklamaya Uymuyor", Durum = "Iade Edildi", Tarih = new DateTime(2026, 3, 13) },
                new() { IadeNo = "IAD-2007", SiparisNo = "SIP-1022", Musteri = "Emine Yildiz", Platform = "Trendyol", Tutar = 320.00m, Sebep = "Eksik Parca", Durum = "Beklemede", Tarih = new DateTime(2026, 3, 12) },
            ]);

            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Iade listesi yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    partial void OnSelectedStatusChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Returns.Clear();
        var filtered = _allItems.AsEnumerable();

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(r => r.Durum == SelectedStatus);

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.IadeNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.SiparisNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Musteri.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var item in filtered)
            Returns.Add(item);

        TotalCount = Returns.Count;
        IsEmpty = Returns.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class ReturnListItemDto
{
    public string IadeNo { get; set; } = string.Empty;
    public string SiparisNo { get; set; } = string.Empty;
    public string Musteri { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string Sebep { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
}
