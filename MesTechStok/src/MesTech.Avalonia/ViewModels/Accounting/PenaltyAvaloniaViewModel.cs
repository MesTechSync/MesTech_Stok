using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class PenaltyAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalPenaltyAmount = "0,00 TL";
    [ObservableProperty] private string pendingAmount = "0,00 TL";
    [ObservableProperty] private string paidAmount = "0,00 TL";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedSource;

    public ObservableCollection<PenaltyItemDto> Items { get; } = [];
    private List<PenaltyItemDto> _allItems = [];

    public ObservableCollection<string> Sources { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "eBay", "Vergi Dairesi", "SGK", "Gumruk", "Diger"];

    public PenaltyAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        SelectedSource = "Tumu";
    }

    public override async Task LoadAsync()
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
                new() { Source = "Trendyol", Description = "Gec kargo cezasi - Siparis #TR-2026-4521", Amount = 150.00m, AmountFormatted = "150,00 TL", PenaltyDate = "2026-03-15", DueDate = "2026-04-15", Status = "Beklemede", ReferenceNumber = "TY-PEN-001" },
                new() { Source = "Hepsiburada", Description = "Iptal orani asimi cezasi", Amount = 320.00m, AmountFormatted = "320,00 TL", PenaltyDate = "2026-03-10", DueDate = "2026-04-10", Status = "Beklemede", ReferenceNumber = "HB-PEN-042" },
                new() { Source = "SGK", Description = "Gecikme zammi - 2026/02 donem", Amount = 1250.00m, AmountFormatted = "1.250,00 TL", PenaltyDate = "2026-03-01", DueDate = "2026-03-31", Status = "Odendi", ReferenceNumber = "SGK-2026-0312" },
                new() { Source = "Vergi Dairesi", Description = "KDV beyanname gecikme cezasi", Amount = 890.00m, AmountFormatted = "890,00 TL", PenaltyDate = "2026-02-28", DueDate = "2026-03-28", Status = "Beklemede", ReferenceNumber = "VD-2026-0228" },
            ];

            var total = _allItems.Sum(x => x.Amount);
            var paid = _allItems.Where(x => x.Status == "Odendi").Sum(x => x.Amount);
            var pending = total - paid;

            TotalPenaltyAmount = $"{total:N2} TL";
            PaidAmount = $"{paid:N2} TL";
            PendingAmount = $"{pending:N2} TL";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Ceza kayitlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedSourceChanged(string? value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Description.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.ReferenceNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedSource) && SelectedSource != "Tumu")
        {
            filtered = filtered.Where(x => x.Source == SelectedSource);
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PenaltyItemDto
{
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string PenaltyDate { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}
