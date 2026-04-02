using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Komisyon Oranları ViewModel — wired to GetPlatformCommissionRatesQuery via MediatR.
/// Platform bazlı komisyon yönetimi (Zincir 6).
/// </summary>
public partial class CommissionRatesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<CommissionRateItem> _allItems = [];

    public ObservableCollection<CommissionRateItem> CommissionRates { get; } = [];

    public CommissionRatesViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            _allItems.Clear();

            var result = await _mediator.Send(
                new GetPlatformCommissionRatesQuery(_tenantProvider.GetCurrentTenantId()));

            foreach (var c in result)
            {
                _allItems.Add(new(
                    c.Platform,
                    c.Rate,
                    c.MinAmount ?? 0m,
                    c.EffectiveFrom.ToString("dd.MM.yyyy"),
                    c.EffectiveTo?.ToString("dd.MM.yyyy") ?? "-"));
            }

            ApplyFilter();
        }, "Komisyon oranları");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        CommissionRates.Clear();
        var filtered = _allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.PlatformName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var item in filtered)
            CommissionRates.Add(item);
        IsEmpty = CommissionRates.Count == 0;
    }

    [RelayCommand]
    private void SaveRate(CommissionRateItem rate)
    {
    }
}

public class CommissionRateItem
{
    public CommissionRateItem(string platform, decimal rate, decimal fixedAmount,
        string effectiveFrom, string effectiveTo)
    {
        PlatformName = platform;
        Rate = rate;
        FixedAmount = fixedAmount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public string PlatformName { get; }
    public decimal Rate { get; set; }
    public decimal FixedAmount { get; set; }
    public string EffectiveFrom { get; }
    public string EffectiveTo { get; }

    public string RateText => $"%{Rate:F1}";
    public string FixedAmountText => FixedAmount > 0 ? $"{FixedAmount:N2} TL" : "-";
    public string EffectivePeriod => $"{EffectiveFrom} — {EffectiveTo}";
    public string ExampleCalculation
    {
        get
        {
            var commission = 100m * Rate / 100m + FixedAmount;
            return $"100 TL satis → {commission:N2} TL komisyon";
        }
    }
}
