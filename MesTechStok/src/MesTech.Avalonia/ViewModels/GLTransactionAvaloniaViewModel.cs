using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class GLTransactionAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedAccount = "Tum Hesaplar";
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;

    public ObservableCollection<GLTransactionItemDto> Transactions { get; } = [];
    private List<GLTransactionItemDto> _allItems = [];

    public ObservableCollection<string> Accounts { get; } =
    [
        "Tum Hesaplar",
        "100 - Kasa",
        "102 - Bankalar",
        "120 - Alicilar",
        "320 - Saticilar",
        "600 - Yurtici Satislar",
        "621 - Satilan Mal Maliyeti",
        "770 - Genel Yonetim Giderleri"
    ];

    public GLTransactionAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            _allItems =
            [
                new() { Date = "19.03.2026", VoucherNo = "YMF-001", Account = "100 - Kasa", Description = "Kasa acilis bakiyesi", DebitFormatted = "10.000,00", CreditFormatted = "", BalanceFormatted = "10.000,00 B" },
                new() { Date = "19.03.2026", VoucherNo = "YMF-002", Account = "600 - Yurtici Satislar", Description = "Trendyol satis", DebitFormatted = "", CreditFormatted = "4.520,00", BalanceFormatted = "4.520,00 A" },
                new() { Date = "19.03.2026", VoucherNo = "YMF-002", Account = "120 - Alicilar", Description = "Trendyol alacak", DebitFormatted = "4.520,00", CreditFormatted = "", BalanceFormatted = "4.520,00 B" },
                new() { Date = "18.03.2026", VoucherNo = "YMF-003", Account = "770 - Genel Yonetim", Description = "Kargo gideri", DebitFormatted = "380,00", CreditFormatted = "", BalanceFormatted = "380,00 B" },
                new() { Date = "18.03.2026", VoucherNo = "YMF-003", Account = "102 - Bankalar", Description = "Kargo odemesi", DebitFormatted = "", CreditFormatted = "380,00", BalanceFormatted = "9.620,00 B" },
                new() { Date = "17.03.2026", VoucherNo = "YMF-004", Account = "621 - SMM", Description = "Satis maliyeti", DebitFormatted = "2.100,00", CreditFormatted = "", BalanceFormatted = "2.100,00 B" },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Muhasebe hareketleri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedAccountChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Description.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.VoucherNo.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedAccount != "Tum Hesaplar")
            filtered = filtered.Where(x => x.Account == SelectedAccount);

        Transactions.Clear();
        foreach (var item in filtered)
            Transactions.Add(item);

        TotalCount = Transactions.Count;
        IsEmpty = Transactions.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddTransaction()
    {
        // Will open add transaction dialog
    }
}

public class GLTransactionItemDto
{
    public string Date { get; set; } = string.Empty;
    public string VoucherNo { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DebitFormatted { get; set; } = string.Empty;
    public string CreditFormatted { get; set; } = string.Empty;
    public string BalanceFormatted { get; set; } = string.Empty;
}
