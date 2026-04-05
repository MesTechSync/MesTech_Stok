using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cari Hesaplar (Accounts Receivable/Payable) screen.
/// Wired to GetCariHesaplarQuery via MediatR.
/// </summary>
public partial class CariHesaplarAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string searchText = string.Empty;

    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private decimal totalDebit;
    [ObservableProperty] private decimal totalCredit;
    [ObservableProperty] private decimal netBalance;

    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<CariHesapItemDto> Items { get; } = [];
    public ObservableCollection<CariHesapItemDto> FilteredItems { get; } = [];
    public ObservableCollection<string> TypeOptions { get; } = ["Tumu", "Musteri", "Tedarikci"];

    private readonly List<CariHesapItemDto> _allItems = [];

    public CariHesaplarAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetCariHesaplarQuery(TenantId: _tenantProvider.GetCurrentTenantId()), ct);

            _allItems.Clear();
            _allItems.AddRange(result.Select(c => new CariHesapItemDto
            {
                HesapAdi = c.Name,
                Tip = c.Type.ToString() == "Customer" ? "Musteri" : "Tedarikci",
                Borc = 0m,
                Alacak = 0m,
                Bakiye = 0m
            }));

            ApplyFilter();
        }, "Cari hesaplar yuklenirken hata");
    }

    partial void OnSelectedTypeChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allItems.AsEnumerable();
        if (SelectedType != "Tumu")
            filtered = filtered.Where(x => x.Tip == SelectedType);
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(x =>
                x.HesapAdi.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                x.Tip.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // Sort
        filtered = SortColumn switch
        {
            "HesapAdi"  => SortAscending ? filtered.OrderBy(x => x.HesapAdi)  : filtered.OrderByDescending(x => x.HesapAdi),
            "Tip"       => SortAscending ? filtered.OrderBy(x => x.Tip)        : filtered.OrderByDescending(x => x.Tip),
            "Borc"      => SortAscending ? filtered.OrderBy(x => x.Borc)       : filtered.OrderByDescending(x => x.Borc),
            "Alacak"    => SortAscending ? filtered.OrderBy(x => x.Alacak)     : filtered.OrderByDescending(x => x.Alacak),
            "Bakiye"    => SortAscending ? filtered.OrderBy(x => x.Bakiye)     : filtered.OrderByDescending(x => x.Bakiye),
            _           => filtered
        };

        FilteredItems.Clear();
        foreach (var item in filtered)
            FilteredItems.Add(item);

        TotalCount = FilteredItems.Count;
        IsEmpty = FilteredItems.Count == 0;
        TotalDebit = FilteredItems.Sum(x => x.Borc);
        TotalCredit = FilteredItems.Sum(x => x.Alacak);
        NetBalance = TotalDebit - TotalCredit;
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
                new ExportReportCommand(Guid.Empty, "cari-hesaplar", "xlsx"), ct);

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
}

public class CariHesapItemDto
{
    public string HesapAdi { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}
