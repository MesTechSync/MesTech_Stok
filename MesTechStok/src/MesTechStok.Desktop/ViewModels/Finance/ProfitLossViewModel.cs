using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Finance;

/// <summary>
/// H29 — Kar/Zarar raporu ViewModel.
/// GetProfitLossQuery uzerinden aylık K/Z verisi ceker, prev/next ile donem degistirir.
/// </summary>
public partial class ProfitLossViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private decimal totalRevenue;
    [ObservableProperty] private decimal totalExpenses;
    [ObservableProperty] private decimal netProfit;
    [ObservableProperty] private decimal profitMargin;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string periodLabel = string.Empty;

    private int _year = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;

    public ProfitLossViewModel(IMediator mediator, ICurrentUserService currentUser)
        => (_mediator, _currentUser) = (mediator, currentUser);

    public async Task LoadAsync()
    {
        IsLoading = true;
        PeriodLabel = new DateTime(_year, _month, 1).ToString("MMMM yyyy",
            new System.Globalization.CultureInfo("tr-TR"));
        try
        {
            var result = await _mediator.Send(
                new GetProfitLossQuery(_currentUser.TenantId, _year, _month));

            TotalRevenue = result.TotalRevenue;
            TotalExpenses = result.TotalExpenses;
            NetProfit = result.NetProfit;
            ProfitMargin = result.ProfitMarginPercent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ProfitLossViewModel] LoadAsync error: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PrevMonth()
    {
        _month--;
        if (_month < 1) { _month = 12; _year--; }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextMonth()
    {
        _month++;
        if (_month > 12) { _month = 1; _year++; }
        await LoadAsync();
    }
}
