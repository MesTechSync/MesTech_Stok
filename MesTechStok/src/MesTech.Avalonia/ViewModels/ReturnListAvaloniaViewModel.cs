using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Return List screen — I-05 Siparis/Kargo Celiklestirme.
/// Displays return requests with status filtering and search.
/// HH-FIX-018 / HH-FIX-return: sort + date filter + Excel export added.
/// </summary>
public partial class ReturnListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string selectedDateRange = "Bu Ay";

    public string[] DateRangeOptions { get; } = ["Tumu", "Bugun", "Bu Hafta", "Bu Ay", "Son 3 Ay"];

    private readonly List<ReturnListItemDto> _allItems = [];

    public ObservableCollection<ReturnListItemDto> Returns { get; } = [];

    public ObservableCollection<string> StatusFilters { get; } =
    [
        "Tumu", "Beklemede", "Onaylandi", "Reddedildi", "Yolda", "Teslim Alindi", "Iade Edildi", "Iptal"
    ];

    public ReturnListAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        ApplyQuickRange("Bu Ay");
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetReturnListQuery(_currentUser.TenantId), ct) ?? [];

            _allItems.Clear();
            _allItems.AddRange(result.Select(r => new ReturnListItemDto
            {
                IadeNo = r.Id.ToString("N")[..8].ToUpper(),
                SiparisNo = r.OrderNumber ?? string.Empty,
                Musteri = string.Empty,
                Platform = string.Empty,
                Tutar = r.RefundAmount,
                Sebep = r.Reason ?? string.Empty,
                Durum = r.Status,
                Tarih = r.CreatedAt
            }));

            ApplyFilter();
        }, "Iade listesi yuklenirken hata");
    }

    partial void OnSelectedStatusChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedDateRangeChanged(string value)
    {
        ApplyQuickRange(value);
        ApplyFilter();
    }

    private void ApplyQuickRange(string range)
    {
        var now = DateTimeOffset.Now;
        switch (range)
        {
            case "Bugun":
                StartDate = now.Date;
                EndDate = now;
                break;
            case "Bu Hafta":
                StartDate = now.AddDays(-(int)now.DayOfWeek);
                EndDate = now;
                break;
            case "Bu Ay":
                StartDate = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
                EndDate = now;
                break;
            case "Son 3 Ay":
                StartDate = now.AddMonths(-3);
                EndDate = now;
                break;
            default: // Tumu
                StartDate = null;
                EndDate = null;
                break;
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allItems.AsEnumerable();

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(r => r.Durum == SelectedStatus);

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(r =>
                r.IadeNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.SiparisNo.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Musteri.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (StartDate.HasValue)
            filtered = filtered.Where(r => r.Tarih >= StartDate.Value.DateTime);

        if (EndDate.HasValue)
            filtered = filtered.Where(r => r.Tarih <= EndDate.Value.DateTime);

        var list = filtered.ToList();

        list = SortColumn switch
        {
            "IadeNo"    => SortAscending ? [.. list.OrderBy(r => r.IadeNo)]    : [.. list.OrderByDescending(r => r.IadeNo)],
            "SiparisNo" => SortAscending ? [.. list.OrderBy(r => r.SiparisNo)] : [.. list.OrderByDescending(r => r.SiparisNo)],
            "Musteri"   => SortAscending ? [.. list.OrderBy(r => r.Musteri)]   : [.. list.OrderByDescending(r => r.Musteri)],
            "Tutar"     => SortAscending ? [.. list.OrderBy(r => r.Tutar)]     : [.. list.OrderByDescending(r => r.Tutar)],
            "Durum"     => SortAscending ? [.. list.OrderBy(r => r.Durum)]     : [.. list.OrderByDescending(r => r.Durum)],
            "Tarih"     => SortAscending ? [.. list.OrderBy(r => r.Tarih)]     : [.. list.OrderByDescending(r => r.Tarih)],
            _           => [.. list.OrderByDescending(r => r.Tarih)]
        };

        Returns.Clear();
        foreach (var item in list)
            Returns.Add(item);

        TotalCount = Returns.Count;
        IsEmpty = Returns.Count == 0;
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
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "returns", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Iadeler disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class ReturnListItemDto
{
    public string IadeNo { get; set; } = string.Empty;
    public string SiparisNo { get; set; } = string.Empty;
    public string Musteri { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string Sebep { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
}
