using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Domain.Enums;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// 3-adimli fatura olusturma sihirbazi ViewModel.
/// Step 1: Siparis secimi, Step 2: Fatura detaylari, Step 3: Onizleme ve onay.
/// INavigationAware: OrderDetail'den OrderId/CustomerName/TotalAmount alir.
/// </summary>
public partial class InvoiceCreateAvaloniaViewModel : ViewModelBase, INavigationAware
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private Guid? _preselectedOrderId;

    public InvoiceCreateAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public Task OnNavigatedToAsync(IDictionary<string, object?> parameters)
    {
        if (parameters.TryGetValue("OrderId", out var idObj) && idObj is Guid id)
            _preselectedOrderId = id;
        if (parameters.TryGetValue("CustomerName", out var nameObj) && nameObj is string name)
            RecipientName = name;
        if (parameters.TryGetValue("TotalAmount", out var amtObj) && amtObj is decimal amt)
            PreviewTotal = amt;
        if (parameters.TryGetValue("TaxAmount", out var taxObj) && taxObj is decimal tax)
            PreviewKdv = tax;
        return LoadAsync();
    }

    [ObservableProperty] private string _statusMessage = string.Empty;
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
        await SafeExecuteAsync(async ct =>
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var orders = await _mediator.Send(
                new GetOrderListQuery(tenantId, 50), ct);

            Orders.Clear();
            foreach (var o in orders)
            {
                Orders.Add(new()
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName ?? "-",
                    Amount = o.TotalAmount,
                    Date = o.OrderDate,
                    Platform = o.SourcePlatform ?? "-",
                    IsSelected = _preselectedOrderId.HasValue && o.Id == _preselectedOrderId.Value
                });
            }

            // Pre-selected order varsa direkt Step 2'ye geç
            if (_preselectedOrderId.HasValue && Orders.Any(o => o.IsSelected))
            {
                CurrentStep = 2;
                UpdateWizardState();
            }

            IsEmpty = Orders.Count == 0;
        }, "Fatura olusturma siparisleri yuklenirken hata");
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
            var scenario = SelectedType == "e-Arsiv" ? EInvoiceScenario.EARSIVFATURA : EInvoiceScenario.TEMELFATURA;
            var type = EInvoiceType.SATIS;
            var selectedOrder = Orders.FirstOrDefault(o => o.IsSelected);
            var orderId = selectedOrder?.Id != Guid.Empty ? selectedOrder?.Id : null;
            await _mediator.Send(new CreateEInvoiceCommand(
                orderId,
                RecipientVkn,
                RecipientName,
                null,
                scenario,
                type,
                DateTime.Now,
                "TRY",
                [],
                SelectedProvider));
            StatusMessage = "E-Fatura olusturuldu.";
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
                Description = $"Siparis {order.OrderNumber} — {order.CustomerName}",
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
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
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
