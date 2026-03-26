using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Application.Queries.GetProductByBarcode;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

public sealed class BarcodeInputService : IBarcodeInputService
{
    private readonly IMediator _mediator;
    private readonly ILogger<BarcodeInputService> _logger;

    public BarcodeInputService(IMediator mediator, ILogger<BarcodeInputService> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BarcodeInputResult> ProcessScanAsync(
        Guid tenantId,
        string barcode,
        BarcodeScanMode mode,
        string? deviceId = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        _logger.LogInformation(
            "Barkod tarandi: {Barcode}, Mod={Mode}, Tenant={TenantId}, Device={DeviceId}",
            barcode, mode, tenantId, deviceId);

        // 1. Scan log kaydet
        await _mediator.Send(new CreateBarcodeScanLogCommand(
            Barcode: barcode,
            Format: DetectFormat(barcode),
            Source: mode.ToString(),
            DeviceId: deviceId), ct).ConfigureAwait(false);

        // 2. Urunu bul
        var product = await _mediator.Send(new GetProductByBarcodeQuery(barcode), ct)
            .ConfigureAwait(false);

        if (product is null)
        {
            _logger.LogWarning("Barkod ile urun bulunamadi: {Barcode}, Mod={Mode}", barcode, mode);
            return BarcodeInputResult.NotFound(barcode, mode);
        }

        _logger.LogInformation(
            "Barkod eslesti: {Barcode} → {ProductName} (SKU={SKU}), Mod={Mode}",
            barcode, product.Name, product.SKU, mode);

        return BarcodeInputResult.Success(product, mode, barcode);
    }

    private static string DetectFormat(string barcode)
    {
        return barcode.Length switch
        {
            8 => "EAN-8",
            12 => "UPC-A",
            13 => "EAN-13",
            14 => "ITF-14",
            _ => "Code128"
        };
    }
}
