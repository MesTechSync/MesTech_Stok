using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Karlilik ViewModel — urun bazli kar analizi + tarih filtre + ozet satir.
/// </summary>
public partial class DropshipProfitAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // Date range filter
    [ObservableProperty] private DateTimeOffset startDate = DateTimeOffset.Now.AddDays(-30);
    [ObservableProperty] private DateTimeOffset endDate = DateTimeOffset.Now;

    // Summary
    [ObservableProperty] private decimal totalRevenue;
    [ObservableProperty] private decimal totalCost;
    [ObservableProperty] private decimal totalCommission;
    [ObservableProperty] private decimal totalNetProfit;
    [ObservableProperty] private decimal overallMargin;

    public ObservableCollection<DropshipProfitItemDto> Items { get; } = [];
    private List<DropshipProfitItemDto> _allItems = [];

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false; // highest profit first

    public DropshipProfitAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetDropshipProfitabilityQuery(_currentUser.TenantId), ct);

            _allItems = result.Select(dto => new DropshipProfitItemDto
            {
                ProductName = dto.ProductName,
                QuantitySold = dto.QuantitySold,
                CustomerPrice = dto.CustomerPrice,
                SupplierPrice = dto.SupplierPrice,
                Commission = dto.CommissionAmount,
                NetProfit = dto.NetProfit,
                MarginPercent = dto.ProfitMargin
            }).ToList();

            TotalRevenue = _allItems.Sum(i => i.CustomerPrice * i.QuantitySold);
            TotalCost = _allItems.Sum(i => i.SupplierPrice * i.QuantitySold);
            TotalCommission = _allItems.Sum(i => i.Commission * i.QuantitySold);
            TotalNetProfit = _allItems.Sum(i => i.NetProfit * i.QuantitySold);
            OverallMargin = TotalRevenue > 0 ? Math.Round(TotalNetProfit / TotalRevenue * 100, 1) : 0;

            ApplySort();
        }, "Dropship karlilik verileri yuklenirken hata");
    }

    private void ApplySort()
    {
        var sorted = SortColumn switch
        {
            "ProductName"   => SortAscending ? _allItems.OrderBy(x => x.ProductName).ToList()   : _allItems.OrderByDescending(x => x.ProductName).ToList(),
            "QuantitySold"  => SortAscending ? _allItems.OrderBy(x => x.QuantitySold).ToList()  : _allItems.OrderByDescending(x => x.QuantitySold).ToList(),
            "NetProfit"     => SortAscending ? _allItems.OrderBy(x => x.NetProfit).ToList()     : _allItems.OrderByDescending(x => x.NetProfit).ToList(),
            "MarginPercent" => SortAscending ? _allItems.OrderBy(x => x.MarginPercent).ToList() : _allItems.OrderByDescending(x => x.MarginPercent).ToList(),
            _               => SortAscending ? _allItems.OrderBy(x => x.NetProfit).ToList()     : _allItems.OrderByDescending(x => x.NetProfit).ToList(),
        };
        Items.Clear();
        foreach (var item in sorted) Items.Add(item);
        TotalCount = Items.Count;
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplySort();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "dropship-profit", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Dropship karlilik verileri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class DropshipProfitItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal CustomerPrice { get; set; }
    public decimal SupplierPrice { get; set; }
    public decimal Commission { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MarginPercent { get; set; }
}
