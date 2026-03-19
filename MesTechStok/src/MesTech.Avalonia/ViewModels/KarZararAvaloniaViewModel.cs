using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class KarZararAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

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

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            // Sample data
            var revenue = 125480m;
            var expenses = 87320m;
            var profit = revenue - expenses;
            var margin = revenue > 0 ? profit / revenue * 100 : 0;

            TotalRevenue = $"{revenue:N2} TL";
            TotalExpenses = $"{expenses:N2} TL";
            NetProfit = $"{profit:N2} TL";
            ProfitMarginText = $"%{margin:N1}";

            LineItems.Clear();
            var items = new List<KarZararLineItemDto>
            {
                new() { Name = "Yurtici Satislar", Type = "Gelir", AmountFormatted = "98.240,00 TL", PercentFormatted = "%78,3" },
                new() { Name = "Pazaryeri Satislari", Type = "Gelir", AmountFormatted = "27.240,00 TL", PercentFormatted = "%21,7" },
                new() { Name = "Satilan Mal Maliyeti", Type = "Gider", AmountFormatted = "-52.400,00 TL", PercentFormatted = "%60,0" },
                new() { Name = "Kargo Giderleri", Type = "Gider", AmountFormatted = "-12.680,00 TL", PercentFormatted = "%14,5" },
                new() { Name = "Pazaryeri Komisyonlari", Type = "Gider", AmountFormatted = "-8.740,00 TL", PercentFormatted = "%10,0" },
                new() { Name = "Genel Yonetim Giderleri", Type = "Gider", AmountFormatted = "-9.200,00 TL", PercentFormatted = "%10,5" },
                new() { Name = "Personel Giderleri", Type = "Gider", AmountFormatted = "-4.300,00 TL", PercentFormatted = "%4,9" },
            };
            foreach (var item in items)
                LineItems.Add(item);

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
