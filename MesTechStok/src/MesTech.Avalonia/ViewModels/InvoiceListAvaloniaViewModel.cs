using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Avalonia.Services;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura listesi ViewModel — filtreleme, arama, sayfalama.
/// DataGrid: InvoiceNumber, RecipientName, Type (badge), Status (badge), Amount, Platform, Date.
/// </summary>
public partial class InvoiceListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private InvoiceListItemDto? selectedInvoice;

    public InvoiceListAvaloniaViewModel(IMediator mediator, IDialogService dialog)
    {
        _mediator = mediator;
        _dialog = dialog;
    }
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    // HH-FIX-016: Date filter
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string selectedDateRange = "Son 3 Ay";
    public string[] DateRangeOptions { get; } = ["Tumu", "Bugun", "Bu Hafta", "Bu Ay", "Son 3 Ay", "Ozel"];

    // HH-FIX-022: Bulk select
    [ObservableProperty] private int selectedCount;
    [ObservableProperty] private bool hasSelection;

    public ObservableCollection<InvoiceListItemDto> Invoices { get; } = [];

    public ObservableCollection<string> InvoiceTypes { get; } =
    [
        "Tumu", "e-Fatura", "e-Arsiv", "e-Ihracat"
    ];

    public ObservableCollection<string> StatusList { get; } =
    [
        "Tumu", "Taslak", "Gonderildi", "Onayli", "Reddedildi"
    ];

    public ObservableCollection<string> PlatformList { get; } =
    [
        "Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"
    ];

    private List<InvoiceListItemDto> _allInvoices = [];
    private const int PageSize = 20;

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetEInvoicesQuery(
                From: DateTime.UtcNow.AddMonths(-3),
                To: DateTime.UtcNow,
                Status: null,
                ProviderId: null,
                Page: CurrentPage,
                PageSize: PageSize), ct);

            _allInvoices = result.Items.Select(inv => new InvoiceListItemDto
            {
                Id = inv.Id,
                InvoiceNumber = inv.EttnNo,
                RecipientName = inv.BuyerTitle,
                Type = inv.Scenario.ToString().Replace("EFATURA", "e-Fatura").Replace("EARSIVFATURA", "e-Arsiv").Replace("EIHRACAT", "e-Ihracat"),
                Status = inv.Status.ToString(),
                Amount = inv.PayableAmount,
                Platform = inv.ProviderId,
                Date = inv.IssueDate
            }).ToList();

            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            ApplyFilters();
        }, "Faturalar yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.RecipientName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.InvoiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
            filtered = filtered.Where(i => i.Type == SelectedType);

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(i => i.Status == SelectedStatus);

        if (SelectedPlatform != "Tumu")
            filtered = filtered.Where(i => i.Platform == SelectedPlatform);

        // HH-FIX-016: Date filter
        if (StartDate.HasValue)
            filtered = filtered.Where(i => i.Date >= StartDate.Value.DateTime);
        if (EndDate.HasValue)
            filtered = filtered.Where(i => i.Date <= EndDate.Value.DateTime);

        var all = filtered.ToList();
        TotalCount = all.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        if (CurrentPage > TotalPages) CurrentPage = 1;

        // Sort
        all = SortColumn switch
        {
            "InvoiceNumber" => SortAscending ? all.OrderBy(x => x.InvoiceNumber).ToList()     : all.OrderByDescending(x => x.InvoiceNumber).ToList(),
            "RecipientName" => SortAscending ? all.OrderBy(x => x.RecipientName).ToList()     : all.OrderByDescending(x => x.RecipientName).ToList(),
            "Type"          => SortAscending ? all.OrderBy(x => x.Type).ToList()              : all.OrderByDescending(x => x.Type).ToList(),
            "Status"        => SortAscending ? all.OrderBy(x => x.Status).ToList()            : all.OrderByDescending(x => x.Status).ToList(),
            "Amount"        => SortAscending ? all.OrderBy(x => x.Amount).ToList()            : all.OrderByDescending(x => x.Amount).ToList(),
            "Platform"      => SortAscending ? all.OrderBy(x => x.Platform).ToList()          : all.OrderByDescending(x => x.Platform).ToList(),
            "Date"          => SortAscending ? all.OrderBy(x => x.Date).ToList()              : all.OrderByDescending(x => x.Date).ToList(),
            _               => SortAscending ? all.OrderByDescending(x => x.Date).ToList()    : all.OrderBy(x => x.Date).ToList(),
        };

        var paged = all.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        Invoices.Clear();
        foreach (var item in paged)
            Invoices.Add(item);

        IsEmpty = Invoices.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        CurrentPage = 1;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            ApplyFilters();
        }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            ApplyFilters();
        }
    }

    [RelayCommand]
    private async Task CreateInvoice()
    {
        // G013: Fatura oluşturma — InvoiceCreateAvaloniaView'a yönlendir
        await _dialog.ShowInfoAsync("Fatura olusturma ekranina yonlendiriliyorsunuz...", "Yeni Fatura");
    }

    [RelayCommand]
    private async Task CancelInvoice()
    {
        if (SelectedInvoice == null)
        {
            await _dialog.ShowInfoAsync("Lutfen iptal edilecek faturayı secin.", "Fatura Iptal");
            return;
        }

        if (SelectedInvoice.Status == "Taslak" || SelectedInvoice.Status == "Draft")
        {
            await _mediator.Send(new CancelEInvoiceCommand(SelectedInvoice.Id, "Kullanici tarafindan iptal edildi"));
            await LoadAsync();
        }
        else
        {
            await _dialog.ShowInfoAsync("Sadece Taslak durumundaki faturalar iptal edilebilir.", "Fatura Iptal");
        }
    }

    partial void OnSearchTextChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
    partial void OnSelectedTypeChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
    partial void OnSelectedStatusChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }
    partial void OnSelectedPlatformChanged(string value) { if (_allInvoices.Count > 0) ApplyFilters(); }

    // HH-FIX-016: Date range quick setter
    partial void OnSelectedDateRangeChanged(string value)
    {
        var now = DateTime.Now;
        (StartDate, EndDate) = value switch
        {
            "Bugun" => (new DateTimeOffset(now.Date), new DateTimeOffset(now)),
            "Bu Hafta" => (new DateTimeOffset(now.Date.AddDays(-(int)now.DayOfWeek + 1)), new DateTimeOffset(now)),
            "Bu Ay" => (new DateTimeOffset(new DateTime(now.Year, now.Month, 1)), new DateTimeOffset(now)),
            "Son 3 Ay" => (new DateTimeOffset(now.AddMonths(-3)), new DateTimeOffset(now)),
            _ => ((DateTimeOffset?)null, (DateTimeOffset?)null)
        };
        CurrentPage = 1;
        ApplyFilters();
    }

    // HH-FIX-022: Bulk select
    [RelayCommand]
    private void SelectAll() { foreach (var i in Invoices) i.IsSelected = true; SelectedCount = Invoices.Count; HasSelection = true; }

    [RelayCommand]
    private void DeselectAll() { foreach (var i in Invoices) i.IsSelected = false; SelectedCount = 0; HasSelection = false; }

    // HH-FIX-013: Export
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var from = StartDate?.DateTime ?? DateTime.Now.AddMonths(-3);
            var to = EndDate?.DateTime ?? DateTime.Now;
            var result = await _mediator.Send(new MesTech.Application.Features.Reporting.Commands.ExportReport.ExportReportCommand(
                Guid.Empty, "invoices", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Faturalar disa aktarilirken hata");
    }
}

public class InvoiceListItemDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsSelected { get; set; }
}
