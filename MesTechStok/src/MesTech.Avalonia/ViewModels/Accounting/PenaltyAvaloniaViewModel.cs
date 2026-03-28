using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class PenaltyAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

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

    public PenaltyAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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
            var result = await _mediator.Send(new GetPenaltyRecordsQuery(_currentUser.TenantId));

            _allItems = result.Select(p => new PenaltyItemDto
            {
                Source = p.Source.ToString(),
                Description = p.Description,
                Amount = p.Amount,
                AmountFormatted = $"{p.Amount:N2} TL",
                PenaltyDate = p.PenaltyDate.ToString("yyyy-MM-dd"),
                DueDate = p.DueDate?.ToString("yyyy-MM-dd") ?? "-",
                Status = p.PaymentStatus == PaymentStatus.Completed ? "Odendi" : "Beklemede",
                ReferenceNumber = p.ReferenceNumber ?? string.Empty
            }).ToList();

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
