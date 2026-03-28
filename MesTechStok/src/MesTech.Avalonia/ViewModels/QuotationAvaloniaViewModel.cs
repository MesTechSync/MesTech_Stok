using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class QuotationAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalQuotationValue = "0,00 TL";
    [ObservableProperty] private string pendingCount = "0";
    [ObservableProperty] private string acceptedCount = "0";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStatus;

    public ObservableCollection<QuotationListItemDto> Items { get; } = [];
    private List<QuotationListItemDto> _allItems = [];

    public ObservableCollection<string> Statuses { get; } =
        ["Tumu", "Taslak", "Gonderildi", "Kabul Edildi", "Reddedildi", "Faturaya Donusturuldu"];

    public QuotationAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        SelectedStatus = "Tumu";
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with _mediator.Send(new ListQuotationsQuery())

            _allItems =
            [
                new() { QuotationNumber = "TKL-2026-001", CustomerName = "ABC Ticaret Ltd.", GrandTotal = 45000m, GrandTotalFormatted = "45.000,00 TL", QuotationDate = "2026-03-20", ValidUntil = "2026-04-20", Status = "Gonderildi", LineCount = 5 },
                new() { QuotationNumber = "TKL-2026-002", CustomerName = "XYZ Holding A.S.", GrandTotal = 128000m, GrandTotalFormatted = "128.000,00 TL", QuotationDate = "2026-03-18", ValidUntil = "2026-04-18", Status = "Kabul Edildi", LineCount = 12 },
                new() { QuotationNumber = "TKL-2026-003", CustomerName = "Demir Elektronik", GrandTotal = 8500m, GrandTotalFormatted = "8.500,00 TL", QuotationDate = "2026-03-25", ValidUntil = "2026-04-25", Status = "Taslak", LineCount = 2 },
                new() { QuotationNumber = "TKL-2026-004", CustomerName = "Yildiz Insaat", GrandTotal = 67200m, GrandTotalFormatted = "67.200,00 TL", QuotationDate = "2026-03-10", ValidUntil = "2026-03-25", Status = "Reddedildi", LineCount = 8 },
                new() { QuotationNumber = "TKL-2026-005", CustomerName = "Kaya Otomotiv", GrandTotal = 34500m, GrandTotalFormatted = "34.500,00 TL", QuotationDate = "2026-03-22", ValidUntil = "2026-04-22", Status = "Faturaya Donusturuldu", LineCount = 3 },
            ];

            var total = _allItems.Sum(x => x.GrandTotal);
            var pending = _allItems.Count(x => x.Status is "Taslak" or "Gonderildi");
            var accepted = _allItems.Count(x => x.Status == "Kabul Edildi");

            TotalQuotationValue = $"{total:N2} TL";
            PendingCount = pending.ToString();
            AcceptedCount = accepted.ToString();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Teklif verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusChanged(string? value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.QuotationNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.CustomerName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedStatus) && SelectedStatus != "Tumu")
        {
            filtered = filtered.Where(x => x.Status == SelectedStatus);
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

public class QuotationListItemDto
{
    public string QuotationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string GrandTotalFormatted { get; set; } = string.Empty;
    public string QuotationDate { get; set; } = string.Empty;
    public string ValidUntil { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LineCount { get; set; }
}
