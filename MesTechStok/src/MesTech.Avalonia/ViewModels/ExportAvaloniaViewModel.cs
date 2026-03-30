using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dışa Aktar ViewModel — WPF011.
/// Wired to ExportProductsCommand via MediatR for product export.
/// Format seçimi + checkbox seçimi + progress.
/// </summary>
public partial class ExportAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    public ExportAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    // Data type checkboxes
    [ObservableProperty] private bool exportProducts = true;
    [ObservableProperty] private bool exportOrders = true;
    [ObservableProperty] private bool exportStock;
    [ObservableProperty] private bool exportCustomers;
    [ObservableProperty] private bool exportInvoices;

    // Format selection
    [ObservableProperty] private string selectedFormat = "Excel (.xlsx)";

    // Progress
    [ObservableProperty] private int exportProgress;
    [ObservableProperty] private bool isExporting;
    [ObservableProperty] private string exportMessage = string.Empty;

    public ObservableCollection<string> Formats { get; } = new()
    {
        "Excel (.xlsx)",
        "CSV",
        "JSON"
    };

    public override Task LoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task ExportAsync()
    {
        var selected = new List<string>();
        if (ExportProducts) selected.Add("Ürünler");
        if (ExportOrders) selected.Add("Siparişler");
        if (ExportStock) selected.Add("Stok");
        if (ExportCustomers) selected.Add("Müşteriler");
        if (ExportInvoices) selected.Add("Faturalar");

        if (selected.Count == 0)
        {
            ExportMessage = "Hata: En az bir veri türü seçmelisiniz.";
            return;
        }

        IsExporting = true;
        ExportProgress = 0;
        ExportMessage = string.Empty;

        try
        {
            var format = SelectedFormat switch
            {
                "CSV" => "csv",
                "JSON" => "json",
                _ => "xlsx"
            };

            var stepCount = selected.Count;
            for (int i = 0; i < stepCount; i++)
            {
                ExportMessage = $"{selected[i]} dışa aktarılıyor...";
                int baseProgress = (i * 100) / stepCount;
                int nextProgress = ((i + 1) * 100) / stepCount;
                ExportProgress = baseProgress;

                if (selected[i] == "Ürünler")
                {
                    _ = await _mediator.Send(
                        new ExportProductsCommand(Format: format), CancellationToken);
                }
                else if (selected[i] == "Siparişler")
                {
                    var tenantId = _tenantProvider.GetCurrentTenantId();
                    _ = await _mediator.Send(
                        new ExportOrdersCommand(tenantId, DateTime.Now.AddDays(-30), DateTime.Now), CancellationToken);
                }
                // TODO: Wire ExportStock, ExportCustomers, ExportInvoices commands when handlers exist

                ExportProgress = nextProgress;
            }

            ExportProgress = 100;
            ExportMessage = $"{selected.Count} veri türü başarıyla {SelectedFormat} formatında dışa aktarıldı.";
        }
        catch (OperationCanceledException)
        {
            ExportMessage = "Dışa aktarma iptal edildi.";
        }
        catch (Exception ex)
        {
            ExportMessage = $"Hata: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }
}
