using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
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

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

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
        await SafeExecuteAsync(async ct =>
        {
            DateTime? from = FromDate?.DateTime;
            DateTime? to = ToDate?.DateTime;
            string? platform = SelectedPlatform == "Tumu" ? null : SelectedPlatform;

            var result = await _mediator.Send(
                new GetSettlementBatchesQuery(_currentUser.TenantId, from, to, platform), ct);

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
        }, "Settlement verileri yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedPlatformChanged(string value) => _ = LoadAsync();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allBatches.AsEnumerable()
            : _allBatches.Where(b =>
                b.Platform.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                b.Status.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // Sort
        filtered = SortColumn switch
        {
            "Platform"        => SortAscending ? filtered.OrderBy(x => x.Platform)        : filtered.OrderByDescending(x => x.Platform),
            "Status"          => SortAscending ? filtered.OrderBy(x => x.Status)          : filtered.OrderByDescending(x => x.Status),
            "TotalGross"      => SortAscending ? filtered.OrderBy(x => x.TotalGross)      : filtered.OrderByDescending(x => x.TotalGross),
            "TotalCommission" => SortAscending ? filtered.OrderBy(x => x.TotalCommission) : filtered.OrderByDescending(x => x.TotalCommission),
            "TotalNet"        => SortAscending ? filtered.OrderBy(x => x.TotalNet)        : filtered.OrderByDescending(x => x.TotalNet),
            _                 => filtered
        };

        Batches.Clear();
        foreach (var b in filtered)
            Batches.Add(b);
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new ExportReportCommand(Guid.Empty, "settlements", "xlsx"), ct);

            if (result?.FileData.Length > 0)
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MesTech_Exports");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, result.FileName);
                await File.WriteAllBytesAsync(path, result.FileData.ToArray(), ct);
            }
        }, "Excel export sirasinda hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Filter() => await LoadAsync();
}
