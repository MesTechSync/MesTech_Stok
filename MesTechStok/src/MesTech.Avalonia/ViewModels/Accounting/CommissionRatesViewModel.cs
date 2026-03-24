using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Komisyon Oranları ViewModel — Zincir 6.
/// Platform bazlı komisyon yönetimi. DEV 1'in CommissionRate entity'sine bağlanacak.
/// </summary>
public partial class CommissionRatesViewModel : ViewModelBase
{
    public ObservableCollection<CommissionRateItem> CommissionRates { get; } = [];

    public override async Task LoadAsync()
    {
        CommissionRates.Clear();
        await Task.Delay(150);

        CommissionRates.Add(new("Trendyol", 15.0m, 0m, "01.01.2026", "31.12.2026"));
        CommissionRates.Add(new("Hepsiburada", 12.5m, 2.50m, "01.01.2026", "31.12.2026"));
        CommissionRates.Add(new("N11", 14.0m, 0m, "01.01.2026", "31.12.2026"));
        CommissionRates.Add(new("Çiçeksepeti", 18.0m, 3.00m, "01.01.2026", "31.12.2026"));
        CommissionRates.Add(new("Amazon TR", 15.0m, 5.00m, "01.01.2026", "31.12.2026"));
        CommissionRates.Add(new("eBay", 13.0m, 0m, "01.01.2026", "31.12.2026"));
        CommissionRates.Add(new("Pazarama", 10.0m, 0m, "01.03.2026", "31.12.2026"));
        CommissionRates.Add(new("PTT AVM", 8.0m, 1.50m, "01.01.2026", "31.12.2026"));
    }

    [RelayCommand]
    private void SaveRate(CommissionRateItem rate)
    {
        System.Diagnostics.Debug.WriteLine($"[Commission] Kaydet: {rate.PlatformName} %{rate.Rate}");
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
