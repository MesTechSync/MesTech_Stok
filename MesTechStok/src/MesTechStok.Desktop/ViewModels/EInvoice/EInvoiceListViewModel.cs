using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.EInvoice;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.EInvoice;

/// <summary>
/// C-04 — E-Fatura listesi ViewModel.
/// GetEInvoicesQuery ile sayfalı veri çeker; SendEInvoiceCommand / CancelEInvoiceCommand ile fatura işlemleri yapar.
/// </summary>
public partial class EInvoiceListViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // ── filter props ──────────────────────────────────────────────────────
    [ObservableProperty] private DateTime? fromDate;
    [ObservableProperty] private DateTime? toDate;
    [ObservableProperty] private EInvoiceStatus? selectedStatus;
    [ObservableProperty] private string searchText = string.Empty;

    // ── state props ───────────────────────────────────────────────────────
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    // ── pagination ────────────────────────────────────────────────────────
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;

    // ── summary counters ──────────────────────────────────────────────────
    [ObservableProperty] private int acceptedCount;
    [ObservableProperty] private int pendingCount;
    [ObservableProperty] private int errorCount;

    public ObservableCollection<EInvoiceDto> Invoices { get; } = [];

    public EInvoiceListViewModel(IMediator mediator, ICurrentUserService currentUser)
        => (_mediator, _currentUser) = (mediator, currentUser);

    // ── commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var query = new GetEInvoicesQuery(
                From: FromDate,
                To: ToDate,
                Status: SelectedStatus,
                ProviderId: null,
                Page: CurrentPage,
                PageSize: PageSize);

            var result = await _mediator.Send(query);

            Invoices.Clear();
            foreach (var item in result.Items)
                Invoices.Add(item);

            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages > 0 ? result.TotalPages : 1;

            // summary counters
            AcceptedCount = result.Items.Count(x => x.Status == EInvoiceStatus.Accepted);
            PendingCount  = result.Items.Count(x => x.Status is EInvoiceStatus.Sending or EInvoiceStatus.Sent);
            ErrorCount    = result.Items.Count(x => x.Status is EInvoiceStatus.Error or EInvoiceStatus.Rejected);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[EInvoiceListViewModel] LoadAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFilter()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFilter()
    {
        FromDate = null;
        ToDate = null;
        SelectedStatus = null;
        SearchText = string.Empty;
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SendInvoice(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            return;

        IsLoading = true;
        try
        {
            await _mediator.Send(new SendEInvoiceCommand(invoiceId));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[EInvoiceListViewModel] SendInvoice error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelInvoice(Guid invoiceId)
    {
        if (invoiceId == Guid.Empty)
            return;

        IsLoading = true;
        try
        {
            await _mediator.Send(new CancelEInvoiceCommand(invoiceId, "Kullanıcı tarafından iptal edildi"));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"[EInvoiceListViewModel] CancelInvoice error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task FirstPage()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LastPage()
    {
        CurrentPage = TotalPages;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await LoadAsync();
        }
    }
}
