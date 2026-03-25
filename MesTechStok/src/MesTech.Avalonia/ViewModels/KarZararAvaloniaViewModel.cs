using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetKarZarar;

namespace MesTech.Avalonia.ViewModels;

public partial class KarZararAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // KPI
    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalExpenses = "0,00 TL";
    [ObservableProperty] private string netProfit = "0,00 TL";
    [ObservableProperty] private string profitMarginText = "%0,0";
    [ObservableProperty] private string periodLabel = string.Empty;

    private DateTime _currentPeriod = DateTime.Now;

    public ObservableCollection<KarZararLineItemDto> LineItems { get; } = [];

    public KarZararAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
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
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var from = new DateTime(_currentPeriod.Year, _currentPeriod.Month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var dto = await _mediator.Send(new GetKarZararQuery(from, to, Guid.Empty));

            var margin = dto.ToplamGelir > 0 ? dto.NetKar / dto.ToplamGelir * 100 : 0;

            TotalRevenue = $"{dto.ToplamGelir:N2} TL";
            TotalExpenses = $"{dto.ToplamGider:N2} TL";
            NetProfit = $"{dto.NetKar:N2} TL";
            ProfitMarginText = $"%{margin:N1}";

            LineItems.Clear();
            LineItems.Add(new KarZararLineItemDto { Name = "Toplam Gelir", Type = "Gelir", AmountFormatted = $"{dto.ToplamGelir:N2} TL", PercentFormatted = "%100,0" });
            LineItems.Add(new KarZararLineItemDto { Name = "Toplam Gider", Type = "Gider", AmountFormatted = $"-{dto.ToplamGider:N2} TL", PercentFormatted = dto.ToplamGelir > 0 ? $"%{dto.ToplamGider / dto.ToplamGelir * 100:N1}" : "%0,0" });

            IsEmpty = LineItems.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kar/Zarar raporu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

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
    public string AmountFormatted { get; set; } = string.Empty;
    public string PercentFormatted { get; set; } = string.Empty;
}
