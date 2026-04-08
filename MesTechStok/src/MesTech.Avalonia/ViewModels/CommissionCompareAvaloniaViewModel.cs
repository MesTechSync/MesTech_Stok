using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Komisyon Karşılaştırma ViewModel — S2-DEV2-03 (Menü 78).
/// Platform bazlı komisyon oranlarını tablo+karşılaştırma olarak gösterir.
/// GetPlatformCommissionRatesQuery ile veri çeker.
/// </summary>
public partial class CommissionCompareAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private decimal avgRate;
    [ObservableProperty] private decimal minRate;
    [ObservableProperty] private decimal maxRate;

    public string[] PlatformOptions { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti", "Pazarama"];

    public ObservableCollection<PlatformCommissionRateDto> Rates { get; } = [];
    private List<PlatformCommissionRateDto> _allRates = [];

    public CommissionCompareAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetPlatformCommissionRatesQuery(_currentUser.TenantId), ct);

            _allRates = result.ToList();
            ApplyFilters();
        }, "Komisyon oranlari yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) { if (_allRates.Count > 0) ApplyFilters(); }
    partial void OnSelectedPlatformChanged(string value) { if (_allRates.Count > 0) ApplyFilters(); }

    private void ApplyFilters()
    {
        var filtered = _allRates.AsEnumerable();

        if (SelectedPlatform != "Tumu")
            filtered = filtered.Where(r => r.Platform == SelectedPlatform);

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText.ToLowerInvariant();
            filtered = filtered.Where(r =>
                r.Platform.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.CategoryName ?? "").Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        Rates.Clear();
        foreach (var r in list) Rates.Add(r);

        TotalCount = list.Count;
        AvgRate = list.Count > 0 ? list.Average(r => r.Rate) : 0;
        MinRate = list.Count > 0 ? list.Min(r => r.Rate) : 0;
        MaxRate = list.Count > 0 ? list.Max(r => r.Rate) : 0;
        IsEmpty = list.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
