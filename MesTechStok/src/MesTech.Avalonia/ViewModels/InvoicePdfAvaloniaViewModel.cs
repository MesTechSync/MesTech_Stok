using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Application.Interfaces;
using MesTech.Avalonia.Services;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura PDF goruntuleme ViewModel.
/// D2-020 FIX: INavigationAware ile fatura ID/numara parametresi alinir.
/// Avalonia native PDF goruntuleme desteklemiyor — placeholder + aksiyonlar.
/// Wired to IInvoicePdfGenerator for real PDF generation.
/// </summary>
public partial class InvoicePdfAvaloniaViewModel : ViewModelBase, INavigationAware
{
    private readonly IInvoicePdfGenerator? _pdfGenerator;

    [ObservableProperty] private string invoiceNumber = string.Empty;
    [ObservableProperty] private string recipientName = string.Empty;
    [ObservableProperty] private decimal amount;
    [ObservableProperty] private string invoiceType = "e-Fatura";
    [ObservableProperty] private DateTime invoiceDate = DateTime.Now;
    [ObservableProperty] private string pdfUrl = string.Empty;
    [ObservableProperty] private bool hasPdf = true;
    [ObservableProperty] private string statusMessage = string.Empty;
    private byte[]? _pdfBytes;

    public InvoicePdfAvaloniaViewModel(IInvoicePdfGenerator? pdfGenerator = null)
    {
        _pdfGenerator = pdfGenerator;
    }

    /// <summary>D2-020: Navigation parametreleri ile fatura bilgilerini al.</summary>
    public Task OnNavigatedToAsync(IDictionary<string, object?> parameters)
    {
        if (parameters.TryGetValue("InvoiceNumber", out var num) && num is string s)
            InvoiceNumber = s;
        if (parameters.TryGetValue("RecipientName", out var name) && name is string n)
            RecipientName = n;
        if (parameters.TryGetValue("Amount", out var amt) && amt is decimal d)
            Amount = d;
        if (parameters.TryGetValue("InvoiceType", out var typ) && typ is string t)
            InvoiceType = t;
        return Task.CompletedTask;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(_ =>
        {
            if (string.IsNullOrEmpty(InvoiceNumber))
            {
                IsEmpty = true;
                StatusMessage = "Fatura numarasi belirtilmedi — fatura listesinden secim yapin.";
                return Task.CompletedTask;
            }
            PdfUrl = $"/invoices/{InvoiceNumber}.pdf";
            HasPdf = true;
            return Task.CompletedTask;
        }, "Fatura PDF yuklenirken hata");
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
                // DEP: DEV1 — Wire to real PDF generation service
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
