using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecords;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class TaxRecordAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalTaxAmount = "0,00 TL";
    [ObservableProperty] private string paidTaxAmount = "0,00 TL";
    [ObservableProperty] private string pendingTaxAmount = "0,00 TL";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedTaxType;

    public ObservableCollection<TaxRecordItemDto> Items { get; } = [];
    private List<TaxRecordItemDto> _allItems = [];

    public ObservableCollection<string> TaxTypes { get; } =
        ["Tumu", "KDV", "Gelir Vergisi", "Kurumlar Vergisi", "Damga Vergisi", "SGK Primi", "Stopaj", "Diger"];

    public TaxRecordAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        SelectedTaxType = "Tumu";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetTaxRecordsQuery(_currentUser.TenantId), ct) ?? [];

            _allItems = result.Select(t => new TaxRecordItemDto
            {
                TaxType = t.TaxType,
                Period = t.Period,
                Description = $"{t.TaxType} — {t.Period}",
                Amount = t.TaxAmount,
                AmountFormatted = $"{t.TaxAmount:N2} TL",
                DueDate = t.DueDate.ToString("yyyy-MM-dd"),
                Status = t.IsPaid ? "Odendi" : "Beklemede",
                ReferenceNumber = $"{t.TaxType}-{t.Period}"
            }).ToList();

            var total = _allItems.Sum(x => x.Amount);
            var paid = _allItems.Where(x => x.Status == "Odendi").Sum(x => x.Amount);
            var pending = total - paid;

            TotalTaxAmount = $"{total:N2} TL";
            PaidTaxAmount = $"{paid:N2} TL";
            PendingTaxAmount = $"{pending:N2} TL";

            ApplyFilters();
        }, "Vergi kayitlari yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedTaxTypeChanged(string? value) => ApplyFilters();

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

        if (!string.IsNullOrWhiteSpace(SelectedTaxType) && SelectedTaxType != "Tumu")
        {
            filtered = filtered.Where(x => x.TaxType == SelectedTaxType);
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

public class TaxRecordItemDto
{
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}
