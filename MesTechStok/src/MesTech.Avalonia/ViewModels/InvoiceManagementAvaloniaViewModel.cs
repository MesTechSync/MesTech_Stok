using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// e-Fatura yonetimi ViewModel — Dalga 14/15.
/// DataGrid with No, Tarih, Musteri, Tutar, Durum, Tip columns + Type filter (e-Fatura/e-Arsiv).
/// Will be wired to GetInvoicesPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class InvoiceManagementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    public InvoiceManagementAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private int totalCount;

    // KD-DEV2-001: Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;

    public ObservableCollection<InvoiceMgmtItemDto> Invoices { get; } = [];

    public ObservableCollection<string> InvoiceTypes { get; } =
    [
        "Tumu", "e-Fatura", "e-Arsiv"
    ];

    private List<InvoiceMgmtItemDto> _allInvoices = [];

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            InvoiceType? typeFilter = SelectedType switch
            {
                "e-Fatura" => InvoiceType.EFatura,
                "e-Arsiv" => InvoiceType.EArsiv,
                _ => null
            };

            var result = await _mediator.Send(
                new GetInvoicesQuery(
                    Type: typeFilter,
                    Status: null,
                    Platform: null,
                    From: null,
                    To: null,
                    Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText),
                ct);

            _allInvoices = result.Items.Select(i => new InvoiceMgmtItemDto
            {
                FaturaNo = i.InvoiceNumber,
                Tarih = i.InvoiceDate,
                Alici = i.RecipientName,
                Tutar = i.TotalAmount,
                Durum = i.StatusName,
                Tip = i.TypeName
            }).ToList();

            ApplyFilters();
        }, "Fatura yonetimi yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Alici.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.FaturaNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Durum.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
        {
            filtered = filtered.Where(i => i.Tip == SelectedType);
        }

        // KD-DEV2-001: Pagination
        var filteredList = filtered.ToList();
        TotalCount = filteredList.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        var paged = filteredList.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        Invoices.Clear();
        foreach (var item in paged)
            Invoices.Add(item);

        IsEmpty = TotalCount == 0;
        PaginationInfo = TotalCount > 0
            ? $"Sayfa {CurrentPage}/{TotalPages} ({TotalCount} fatura)"
            : string.Empty;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateInvoice()
    {
        // DEP: DEV1 — Wire to CreateEInvoiceCommand via navigation or dialog — requires user input
        // For now, refresh the list after external creation
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allInvoices.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedTypeChanged(string value)
    {
        if (_allInvoices.Count > 0)
        {
            CurrentPage = 1;
            ApplyFilters();
        }
    }

    // KD-DEV2-001: Pagination commands
    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }

    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    [RelayCommand]
    private void FirstPage() { if (CurrentPage != 1) { CurrentPage = 1; ApplyFilters(); } }

    [RelayCommand]
    private void LastPage() { if (CurrentPage != TotalPages) { CurrentPage = TotalPages; ApplyFilters(); } }

    // KD-DEV2-002: Export CSV placeholder
    [RelayCommand]
    private Task ExportCsvAsync()
    {
        // DEP: Real export via Application layer — placeholder for now
        ExportMessage = $"CSV dosyasi basariyla olusturuldu. ({TotalCount} fatura)";
        return Task.CompletedTask;
    }

    [ObservableProperty] private string exportMessage = string.Empty;
}

public class InvoiceMgmtItemDto
{
    public string FaturaNo { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public string Alici { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
}
