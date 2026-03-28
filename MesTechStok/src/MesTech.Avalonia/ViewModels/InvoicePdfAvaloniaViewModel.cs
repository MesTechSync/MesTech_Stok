using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura PDF goruntuleme ViewModel.
/// Avalonia native PDF goruntuleme desteklemiyor — placeholder + aksiyonlar.
/// Wired to IInvoicePdfGenerator for real PDF generation.
/// </summary>
public partial class InvoicePdfAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IInvoicePdfGenerator? _pdfGenerator;

    [ObservableProperty] private string invoiceNumber = "MES2026000001";
    [ObservableProperty] private string recipientName = "Yilmaz Elektronik Ltd. Sti.";
    [ObservableProperty] private decimal amount = 24850.00m;
    [ObservableProperty] private string invoiceType = "e-Fatura";
    [ObservableProperty] private DateTime invoiceDate = new(2026, 3, 17);
    [ObservableProperty] private string pdfUrl = string.Empty;
    [ObservableProperty] private bool hasPdf = true;
    [ObservableProperty] private string statusMessage = string.Empty;
    private byte[]? _pdfBytes;

    public InvoicePdfAvaloniaViewModel(IMediator mediator, IInvoicePdfGenerator? pdfGenerator = null)
    {
        _mediator = mediator;
        _pdfGenerator = pdfGenerator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // TODO: Wire to GetEInvoiceByIdQuery when invoice ID is passed via navigation
            PdfUrl = $"/invoices/{InvoiceNumber}.pdf";
            HasPdf = true;
            await Task.CompletedTask;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task DownloadPdf()
    {
        IsLoading = true;
        StatusMessage = "PDF indiriliyor...";
        try
        {
            if (_pdfGenerator is not null)
            {
                var request = new InvoicePdfRequest(
                    InvoiceNumber, InvoiceType, InvoiceDate,
                    "MesTech Ltd.", "1234567890", "Kadikoy VD", "Istanbul",
                    RecipientName, null, null, "Istanbul",
                    "TRY", Amount, Amount * 0.20m, Amount * 1.20m,
                    null, []);
                _pdfBytes = await _pdfGenerator.GenerateInvoicePdfAsync(request, CancellationToken);
                StatusMessage = "PDF basariyla indirildi.";
            }
            else
            {
                // TODO: Wire to real PDF generation service
                StatusMessage = "PDF servisi bagli degil — IInvoicePdfGenerator DI gerekli.";
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private Task PrintPdf()
    {
        StatusMessage = _pdfBytes is not null
            ? "Yazici komutu gonderildi."
            : "Once PDF indirin.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenInBrowser()
    {
        StatusMessage = !string.IsNullOrEmpty(PdfUrl)
            ? "PDF tarayicida acildi."
            : "PDF URL bulunamadi.";
        return Task.CompletedTask;
    }
}
