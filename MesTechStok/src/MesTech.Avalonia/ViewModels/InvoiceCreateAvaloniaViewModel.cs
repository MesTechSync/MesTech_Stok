using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// 3-adimli fatura olusturma sihirbazi ViewModel.
/// Step 1: Siparis secimi, Step 2: Fatura detaylari, Step 3: Onizleme ve onay.
/// </summary>
public partial class InvoiceCreateAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private int currentStep = 1;

    // Step 1: Order selection
    public ObservableCollection<InvoiceOrderItemDto> Orders { get; } = [];

    // Step 2: Invoice details
    [ObservableProperty] private string selectedType = "e-Fatura";
    [ObservableProperty] private string selectedProvider = "Sovos";
    [ObservableProperty] private int kdvRate = 20;
    [ObservableProperty] private string recipientName = string.Empty;
    [ObservableProperty] private string recipientVkn = string.Empty;
    [ObservableProperty] private string recipientAddress = string.Empty;

    public ObservableCollection<string> InvoiceTypes { get; } =
    [
        "e-Fatura", "e-Arsiv", "e-Ihracat"
    ];

    public ObservableCollection<string> Providers { get; } =
    [
        "Sovos", "GIB Portal", "Foriba", "Logo e-Fatura"
    ];

    public ObservableCollection<int> KdvRates { get; } = [0, 1, 10, 20];

    // Step 3: Preview
    public ObservableCollection<InvoiceLinePreviewDto> PreviewLines { get; } = [];
    [ObservableProperty] private decimal previewSubtotal;
    [ObservableProperty] private decimal previewKdv;
    [ObservableProperty] private decimal previewTotal;

    // Wizard state
    [ObservableProperty] private bool canGoBack;
    [ObservableProperty] private bool canGoNext = true;
    [ObservableProperty] private bool isConfirmStep;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            await Task.Delay(300);

            Orders.Clear();
            Orders.Add(new() { OrderId = "SIP-2026-0451", CustomerName = "Yilmaz Elektronik Ltd. Sti.", Amount = 12450.00m, Date = new DateTime(2026, 3, 17), Platform = "Trendyol", IsSelected = false });
            Orders.Add(new() { OrderId = "SIP-2026-0452", CustomerName = "Demir Bilisim A.S.", Amount = 8320.50m, Date = new DateTime(2026, 3, 16), Platform = "Hepsiburada", IsSelected = false });
            Orders.Add(new() { OrderId = "SIP-2026-0453", CustomerName = "Kaya Ticaret ve Sanayi", Amount = 3250.00m, Date = new DateTime(2026, 3, 15), Platform = "N11", IsSelected = false });
            Orders.Add(new() { OrderId = "SIP-2026-0454", CustomerName = "Arslan Mobilya", Amount = 15400.75m, Date = new DateTime(2026, 3, 14), Platform = "Trendyol", IsSelected = false });
            Orders.Add(new() { OrderId = "SIP-2026-0455", CustomerName = "Celik Otomotiv San.", Amount = 27200.00m, Date = new DateTime(2026, 3, 13), Platform = "Amazon", IsSelected = false });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Siparisler yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void GoNext()
    {
        if (CurrentStep < 3)
        {
            CurrentStep++;
            UpdateWizardState();

            if (CurrentStep == 3)
                BuildPreview();
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateWizardState();
        }
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(500); // Simulate invoice creation
            // Reset wizard
            CurrentStep = 1;
            UpdateWizardState();
        }
        finally { IsLoading = false; }
    }

    private void UpdateWizardState()
    {
        CanGoBack = CurrentStep > 1;
        CanGoNext = CurrentStep < 3;
        IsConfirmStep = CurrentStep == 3;
    }

    private void BuildPreview()
    {
        PreviewLines.Clear();
        var selected = Orders.Where(o => o.IsSelected).ToList();
        decimal subtotal = 0;
        foreach (var order in selected)
        {
            PreviewLines.Add(new()
            {
                Description = $"Siparis {order.OrderId} — {order.CustomerName}",
                Amount = order.Amount
            });
            subtotal += order.Amount;
        }
        PreviewSubtotal = subtotal;
        PreviewKdv = subtotal * KdvRate / 100m;
        PreviewTotal = subtotal + PreviewKdv;
    }
}

public class InvoiceOrderItemDto : ObservableObject
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Platform { get; set; } = string.Empty;

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}

public class InvoiceLinePreviewDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
