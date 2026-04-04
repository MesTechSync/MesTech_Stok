using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.ListQuotations;

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
        await SafeExecuteAsync(async ct =>
        {
            var quotations = await _mediator.Send(new ListQuotationsQuery(), ct);
            _allItems = quotations.Select(q => new QuotationListItemDto
            {
                QuotationNumber = q.QuotationNumber,
                CustomerName = q.CustomerName,
                GrandTotal = q.GrandTotal,
                GrandTotalFormatted = $"{q.GrandTotal:N2} TL",
                QuotationDate = q.QuotationDate.ToString("yyyy-MM-dd"),
                ValidUntil = q.ValidUntil.ToString("yyyy-MM-dd"),
                Status = q.Status,
                LineCount = q.Lines.Count
            }).ToList();

            var total = _allItems.Sum(x => x.GrandTotal);
            var pending = _allItems.Count(x => x.Status is "Taslak" or "Gonderildi");
            var accepted = _allItems.Count(x => x.Status == "Kabul Edildi");

            TotalQuotationValue = $"{total:N2} TL";
            PendingCount = pending.ToString();
            AcceptedCount = accepted.ToString();

            ApplyFilters();
        }, "Teklifler yuklenirken hata");
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
