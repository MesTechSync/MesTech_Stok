using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Fatura PDF goruntuleme ViewModel.
/// Avalonia native PDF goruntuleme desteklemiyor — placeholder + aksiyonlar.
/// </summary>
public partial class InvoicePdfAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string invoiceNumber = "MES2026000001";
    [ObservableProperty] private string recipientName = "Yilmaz Elektronik Ltd. Sti.";
    [ObservableProperty] private decimal amount = 24850.00m;
    [ObservableProperty] private string invoiceType = "e-Fatura";
    [ObservableProperty] private DateTime invoiceDate = new(2026, 3, 17);
    [ObservableProperty] private string pdfUrl = string.Empty;
    [ObservableProperty] private bool hasPdf = true;
    [ObservableProperty] private string statusMessage = string.Empty;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(200);
            PdfUrl = $"/invoices/{InvoiceNumber}.pdf";
            HasPdf = true;
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
            await Task.Delay(500); // Simulate download
            StatusMessage = "PDF basariyla indirildi.";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PrintPdf()
    {
        StatusMessage = "Yaziciya gonderiliyor...";
        await Task.Delay(300);
        StatusMessage = "Yazici komutu gonderildi.";
    }

    [RelayCommand]
    private async Task OpenInBrowser()
    {
        StatusMessage = "Tarayicide aciliyor...";
        await Task.Delay(200);
        StatusMessage = "PDF tarayicida acildi.";
    }
}
