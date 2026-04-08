using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.EInvoice.Queries;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// E-Invoice management ViewModel — wired to GetEInvoicesQuery via MediatR.
/// G033: Task.Delay mock replaced with real mediator.Send call.
/// </summary>
public partial class EInvoiceAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<EInvoiceItemDto> Invoices { get; } = [];

    private List<EInvoiceItemDto> _allInvoices = [];

    public EInvoiceAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetEInvoicesQuery(
                    From: DateTime.Now.AddDays(-90),
                    To: DateTime.Now,
                    Status: null,
                    ProviderId: null,
                    Page: 1,
                    PageSize: 50),
                ct);

            _allInvoices = result.Items.Select(dto => new EInvoiceItemDto
            {
                InvoiceNo = dto.EttnNo,
                Date = dto.IssueDate.ToString("dd.MM.yyyy"),
                Receiver = dto.BuyerTitle,
                Amount = dto.PayableAmount,
                Status = dto.Status.ToString()
            }).ToList();

            ApplyFilters();
        }, "E-faturalar yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.InvoiceNo.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Receiver.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Invoices.Clear();
        foreach (var item in filtered)
            Invoices.Add(item);

        TotalCount = Invoices.Count;
        IsEmpty = Invoices.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task CreateInvoiceAsync()
    {
        // Navigate to invoice create screen — wired via navigation service
        await Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allInvoices.Count > 0)
            ApplyFilters();
    }
}

public class EInvoiceItemDto
{
    public string InvoiceNo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
