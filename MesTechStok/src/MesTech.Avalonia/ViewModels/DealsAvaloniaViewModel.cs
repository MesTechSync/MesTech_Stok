using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class DealsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStage;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string totalAmount = "0 TL";
    [ObservableProperty] private string sortColumn = "DealName";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<DealListItemVm> Deals { get; } = [];
    private List<DealListItemVm> _allItems = [];
    public string[] StageOptions { get; } = ["Tumu", "Ilk Iletisim", "Teklif Verildi", "Muzakere", "Kazanildi", "Kaybedildi"];

    public DealsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetDealsQuery(_currentUser.TenantId, Page: 1, PageSize: 100), ct) ?? new();

            _allItems = result.Items.Select(deal => new DealListItemVm
            {
                Id = deal.Id,
                Title = deal.Title,
                ContactName = deal.ContactName,
                Amount = deal.Amount,
                Stage = deal.StageName,
                Probability = 0,
                CreatedAt = deal.CreatedAt
            }).ToList();

            TotalCount = result.TotalCount;
            ApplyFilters();
        }, "Firsatlar yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilters();
    }

    partial void OnSelectedStageChanged(string? value) => ApplyFilters();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (x.ContactName != null && x.ContactName.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        if (SelectedStage != null && SelectedStage != "Tumu")
            filtered = filtered.Where(x => x.Stage == SelectedStage);

        filtered = SortColumn switch
        {
            "DealName" => SortAscending ? filtered.OrderBy(x => x.Title) : filtered.OrderByDescending(x => x.Title),
            "Amount"   => SortAscending ? filtered.OrderBy(x => x.Amount) : filtered.OrderByDescending(x => x.Amount),
            "Stage"    => SortAscending ? filtered.OrderBy(x => x.Stage) : filtered.OrderByDescending(x => x.Stage),
            "Date"     => SortAscending ? filtered.OrderBy(x => x.CreatedAt) : filtered.OrderByDescending(x => x.CreatedAt),
            _          => SortAscending ? filtered.OrderBy(x => x.Title) : filtered.OrderByDescending(x => x.Title),
        };

        Deals.Clear();
        foreach (var deal in filtered)
            Deals.Add(deal);

        TotalAmount = $"{Deals.Sum(d => d.Amount):N0} TL";
        IsEmpty = Deals.Count == 0;
    }

    [RelayCommand]
    private async Task Add()
    {
        await _dialog.ShowInfoAsync("Bu özellik yakinda aktif olacak.", "MesTech");
    }
}

public class DealListItemVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public DateTime CreatedAt { get; set; }

    public string AmountDisplay => $"{Amount:N0} TL";
    public string ProbabilityDisplay => $"%{Probability}";
}
