using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Settlement (hesap kesimi) ViewModel — platform bazlı ödeme mutabakatı.
/// G6880: 14 parser var ama UI yok — bu view kullanıcıya settlement verilerini gösterir.
/// Wired to GetSettlementBatchesQuery via MediatR.
/// </summary>
public partial class SettlementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // KPI
    [ObservableProperty] private string totalBatches = "0";
    [ObservableProperty] private string totalGrossText = "₺0";
    [ObservableProperty] private string totalCommissionText = "₺0";
    [ObservableProperty] private string totalNetText = "₺0";
    [ObservableProperty] private int totalCount;

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private DateTimeOffset? fromDate;
    [ObservableProperty] private DateTimeOffset? toDate;

    public ObservableCollection<SettlementBatchDto> Batches { get; } = [];
    private List<SettlementBatchDto> _allBatches = [];

    public ObservableCollection<string> Platforms { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "eBay", "Shopify", "WooCommerce", "Pazarama", "PttAvm", "OpenCart", "Etsy", "Ozon", "Zalando", "Bitrix24"];

    public SettlementAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            DateTime? from = FromDate?.DateTime;
            DateTime? to = ToDate?.DateTime;
            string? platform = SelectedPlatform == "Tumu" ? null : SelectedPlatform;

            var result = await _mediator.Send(
                new GetSettlementBatchesQuery(_currentUser.TenantId, from, to, platform));

            _allBatches = result.ToList();
            ApplyFilter();

            var totalGross = _allBatches.Sum(b => b.TotalGross);
            var totalComm = _allBatches.Sum(b => b.TotalCommission);
            var totalNet = _allBatches.Sum(b => b.TotalNet);

            TotalBatches = _allBatches.Count.ToString();
            TotalGrossText = $"₺{totalGross:N2}";
            TotalCommissionText = $"₺{totalComm:N2}";
            TotalNetText = $"₺{totalNet:N2}";
            TotalCount = _allBatches.Count;
            IsEmpty = _allBatches.Count == 0;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Settlement verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedPlatformChanged(string value) => _ = LoadAsync();

    private void ApplyFilter()
    {
        Batches.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allBatches
            : _allBatches.Where(b =>
                b.Platform.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var b in filtered)
            Batches.Add(b);
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Filter() => await LoadAsync();
}
