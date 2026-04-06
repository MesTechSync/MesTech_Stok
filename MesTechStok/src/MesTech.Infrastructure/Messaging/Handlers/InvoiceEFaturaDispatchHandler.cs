using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Handlers;

/// <summary>
/// InvoiceCreatedEvent → VKN mükellef sorgu → e-Fatura veya e-Arşiv otomatik gönderim.
///
/// Zincir: Order → Invoice (DEV1) → [BU HANDLER] → Provider.CreateEFaturaAsync/CreateEArsivAsync → GİB
///
/// Karar mantığı:
///   1. Invoice.CustomerTaxNumber ile IsEInvoiceTaxpayerAsync çağır
///   2. Kayıtlıysa → CreateEFaturaAsync (e-Fatura)
///   3. Kayıtlı değilse → CreateEArsivAsync (e-Arşiv)
///   4. CustomerTaxNumber boşsa (bireysel müşteri) → CreateEArsivAsync
/// </summary>
public sealed class InvoiceEFaturaDispatchHandler
    : INotificationHandler<DomainEventNotification<InvoiceCreatedEvent>>
{
    private readonly IInvoiceProviderFactory _providerFactory;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InvoiceEFaturaDispatchHandler> _logger;

    public InvoiceEFaturaDispatchHandler(
        IInvoiceProviderFactory providerFactory,
        IInvoiceRepository invoiceRepo,
        IUnitOfWork uow,
        ILogger<InvoiceEFaturaDispatchHandler> logger)
    {
        _providerFactory = providerFactory;
        _invoiceRepo = invoiceRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "E-Fatura dispatch: InvoiceId={InvoiceId} OrderId={OrderId} Total={Total}",
            e.InvoiceId, e.OrderId, e.GrandTotal);

        // 1. Fatura entity'sini yükle
        var invoice = await _invoiceRepo.GetByIdAsync(e.InvoiceId, cancellationToken).ConfigureAwait(false);
        if (invoice is null)
        {
            _logger.LogError("E-Fatura dispatch: Invoice {InvoiceId} not found in DB — skipping", e.InvoiceId);
            return;
        }

        // 2. Provider seç (tenant'ın tercih ettiği provider veya default)
        var provider = ResolveProvider(invoice);
        if (provider is null)
        {
            _logger.LogWarning("E-Fatura dispatch: no active IInvoiceProvider configured — skipping. " +
                "Configure InvoiceProvider in tenant settings.");
            return;
        }

        // 3. InvoiceDto oluştur
        var dto = MapToDto(invoice);

        // 4. VKN mükellef sorgusu → e-Fatura veya e-Arşiv karar
        var useEFatura = false;
        if (!string.IsNullOrEmpty(invoice.CustomerTaxNumber) && invoice.CustomerTaxNumber.Length >= 10)
        {
            try
            {
                useEFatura = await provider.IsEInvoiceTaxpayerAsync(
                    invoice.CustomerTaxNumber, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "VKN mükellef sorgu: {VKN} → {Result}",
                    invoice.CustomerTaxNumber, useEFatura ? "e-Fatura (kayıtlı)" : "e-Arşiv (kayıtlı değil)");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "VKN mükellef sorgu başarısız: {VKN} — e-Arşiv ile devam ediliyor (güvenli default)",
                    invoice.CustomerTaxNumber);
            }
        }
        else
        {
            _logger.LogDebug("Müşteri VKN yok veya TCKN (bireysel) — e-Arşiv kesilecek");
        }

        // 5. Gönder
        try
        {
            var result = useEFatura
                ? await provider.CreateEFaturaAsync(dto, cancellationToken).ConfigureAwait(false)
                : await provider.CreateEArsivAsync(dto, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                _logger.LogInformation(
                    "E-Fatura gönderim BAŞARILI: {Type} GibId={GibId} Invoice={InvoiceId}",
                    useEFatura ? "e-Fatura" : "e-Arşiv", result.GibInvoiceId, e.InvoiceId);

                invoice.MarkAsSent(result.GibInvoiceId, result.PdfUrl);
                await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError(
                    "E-Fatura gönderim BAŞARISIZ: {Type} Error={Error} Invoice={InvoiceId}",
                    useEFatura ? "e-Fatura" : "e-Arşiv", result.ErrorMessage, e.InvoiceId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "E-Fatura gönderim EXCEPTION: Invoice={InvoiceId} — InvoiceRetryJob tekrar deneyecek",
                e.InvoiceId);
        }
    }

    private IInvoiceProvider? ResolveProvider(Invoice invoice)
    {
        // Tenant-specific provider tercihine göre seç
        // Şimdilik: ilk mevcut provider'ı kullan
        var providers = _providerFactory.GetAll();
        return providers.FirstOrDefault(p => p.Provider is not (InvoiceProvider.None or InvoiceProvider.Manual));
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        var lines = invoice.Lines?.Select(l => new InvoiceLineDto(
            ProductName: l.ProductName ?? "Ürün",
            SKU: l.SKU,
            Quantity: l.Quantity,
            UnitPrice: l.UnitPrice,
            TaxRate: l.TaxRate,
            TaxAmount: l.TaxAmount,
            LineTotal: l.LineTotal
        )).ToList() ?? [];

        return new InvoiceDto(
            InvoiceNumber: invoice.InvoiceNumber ?? $"INV-{invoice.Id:N}"[..16],
            CustomerName: invoice.CustomerName,
            CustomerTaxNumber: invoice.CustomerTaxNumber,
            CustomerTaxOffice: invoice.CustomerTaxOffice,
            CustomerAddress: invoice.CustomerAddress,
            SubTotal: invoice.SubTotal,
            TaxTotal: invoice.TaxTotal,
            GrandTotal: invoice.GrandTotal,
            Lines: lines
        );
    }
}
