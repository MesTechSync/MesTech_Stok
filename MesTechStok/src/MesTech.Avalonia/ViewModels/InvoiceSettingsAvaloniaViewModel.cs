#pragma warning disable CS1998
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class InvoiceSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;


    // E-Fatura
    [ObservableProperty] private string selectedProvider = "Sovos (Foriba)";
    [ObservableProperty] private bool isEInvoiceActive = true;
    [ObservableProperty] private bool autoCreateEArchive = true;

    // Company
    [ObservableProperty] private string companyName = string.Empty;
    [ObservableProperty] private string taxOffice = string.Empty;
    [ObservableProperty] private string taxNumber = string.Empty;
    [ObservableProperty] private string companyAddress = string.Empty;

    // Numbering
    [ObservableProperty] private string invoicePrefix = "MES";
    [ObservableProperty] private int nextInvoiceNumber = 1;

    // VAT
    [ObservableProperty] private int defaultVatRate = 20;
    [ObservableProperty] private bool pricesIncludeVat = true;

    public ObservableCollection<string> InvoiceProviders { get; } =
    [
        "Sovos (Foriba)",
        "GIB Portal",
        "E-Logo",
        "EDM Bilisim",
        "Turkcell",
        "Innova (PayFlex)",
        "Uyumsoft",
        "DTP (Digital Planet)"
    ];

    public InvoiceSettingsAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var settings = await _mediator.Send(new GetInvoiceSettingsQuery(tenantId), CancellationToken);

            SelectedProvider = settings.DefaultProvider;
            InvoicePrefix = settings.InvoicePrefix ?? "MES";
            NextInvoiceNumber = settings.NextInvoiceNumber;
            DefaultVatRate = (int)(settings.DefaultTaxRate * 100);
            AutoCreateEArchive = settings.AutoApprove;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Fatura ayarlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task Save()
    {
        IsLoading = true;
        try
        {
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Fatura ayarlari kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
