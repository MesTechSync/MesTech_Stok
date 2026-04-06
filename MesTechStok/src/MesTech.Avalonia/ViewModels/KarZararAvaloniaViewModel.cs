using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class KarZararAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    // KPI
    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalExpenses = "0,00 TL";
    [ObservableProperty] private string netProfit = "0,00 TL";
    [ObservableProperty] private string profitMarginText = "%0,0";
    [ObservableProperty] private string periodLabel = string.Empty;
    [ObservableProperty] private string sortColumn = "Name";
    [ObservableProperty] private bool sortAscending = true;

    private DateTime _currentPeriod = DateTime.Now;

    public ObservableCollection<KarZararLineItemDto> LineItems { get; } = [];
    private List<KarZararLineItemDto> _allItems = [];

    public KarZararAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        UpdatePeriodLabel();
    }

    private void UpdatePeriodLabel()
    {
        var months = new[] { "", "Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran",
            "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik" };
        PeriodLabel = $"{months[_currentPeriod.Month]} {_currentPeriod.Year}";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var from = new DateTime(_currentPeriod.Year, _currentPeriod.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var dto = await _mediator.Send(new GetKarZararQuery(from, to, _currentUser.TenantId), ct);

            var margin = dto.ToplamGelir > 0 ? dto.NetKar / dto.ToplamGelir * 100 : 0;

            TotalRevenue = $"{dto.ToplamGelir:N2} TL";
            TotalExpenses = $"{dto.ToplamGider:N2} TL";
            NetProfit = $"{dto.NetKar:N2} TL";
            ProfitMarginText = $"%{margin:N1}";

            _allItems =
            [
                new KarZararLineItemDto { Name = "Toplam Gelir", Type = "Gelir", AmountRaw = dto.ToplamGelir, AmountFormatted = $"{dto.ToplamGelir:N2} TL", PercentFormatted = "%100,0" },
                new KarZararLineItemDto { Name = "Toplam Gider", Type = "Gider", AmountRaw = dto.ToplamGider, AmountFormatted = $"-{dto.ToplamGider:N2} TL", PercentFormatted = dto.ToplamGelir > 0 ? $"%{dto.ToplamGider / dto.ToplamGelir * 100:N1}" : "%0,0" },
            ];

            ApplyFilters();
        }, "Kar/Zarar raporu yuklenirken hata");
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

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        filtered = SortColumn switch
        {
            "Name"    => SortAscending ? filtered.OrderBy(x => x.Name) : filtered.OrderByDescending(x => x.Name),
            "Type"    => SortAscending ? filtered.OrderBy(x => x.Type) : filtered.OrderByDescending(x => x.Type),
            "Amount"  => SortAscending ? filtered.OrderBy(x => x.AmountRaw) : filtered.OrderByDescending(x => x.AmountRaw),
            "Percent" => SortAscending ? filtered.OrderBy(x => x.PercentFormatted) : filtered.OrderByDescending(x => x.PercentFormatted),
            _         => SortAscending ? filtered.OrderBy(x => x.Name) : filtered.OrderByDescending(x => x.Name),
        };

        LineItems.Clear();
        foreach (var item in filtered)
            LineItems.Add(item);

        IsEmpty = LineItems.Count == 0;
    }

    [RelayCommand]
    private async Task PrevMonth()
    {
        _currentPeriod = _currentPeriod.AddMonths(-1);
        UpdatePeriodLabel();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextMonth()
    {
        _currentPeriod = _currentPeriod.AddMonths(1);
        UpdatePeriodLabel();
        await LoadAsync();
    }
}

public class KarZararLineItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal AmountRaw { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string PercentFormatted { get; set; } = string.Empty;
}
