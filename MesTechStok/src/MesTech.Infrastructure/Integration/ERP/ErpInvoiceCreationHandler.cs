using MediatR;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// EMR-14 GOREV S-02: InvoiceGeneratedForERPEvent → otomatik ERP fatura olusturma.
/// Siparis tamamlanip fatura kesildiginde hedef ERP'ye musteri hesabi + fatura push eder.
///
/// Akis:
///   1. InvoiceGeneratedForERPEvent dinle
///   2. Fatura + siparis verisini repository'den cek
///   3. Store ERP provider'ini resolve et (StoreCredential "ErpProvider" key)
///   4. IErpAdapterFactory ile adapter olustur
///   5. IErpAccountCapable ise → musteri cari hesap kontrol/olustur
///   6. IErpInvoiceCapable ise → fatura olustur
///   7. ErpSyncLog ile tum islemleri logla
///
/// Hata politikasi: ERP sync hatasi siparis akisini KIRMAZ — hata loglanir, sync log'a yazilir.
/// </summary>
public sealed class ErpInvoiceCreationHandler
    : INotificationHandler<DomainEventNotification<InvoiceGeneratedForERPEvent>>
{
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IErpAdapterFactory _erpAdapterFactory;
    private readonly IErpSyncLogRepository _syncLogRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ErpInvoiceCreationHandler> _logger;

    public ErpInvoiceCreationHandler(
        IInvoiceRepository invoiceRepo,
        IOrderRepository orderRepo,
        IErpAdapterFactory erpAdapterFactory,
        IErpSyncLogRepository syncLogRepo,
        IUnitOfWork uow,
        ILogger<ErpInvoiceCreationHandler> logger)
    {
        _invoiceRepo = invoiceRepo ?? throw new ArgumentNullException(nameof(invoiceRepo));
        _orderRepo = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));
        _erpAdapterFactory = erpAdapterFactory ?? throw new ArgumentNullException(nameof(erpAdapterFactory));
        _syncLogRepo = syncLogRepo ?? throw new ArgumentNullException(nameof(syncLogRepo));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ERP sync hatasi siparis akisini kirmamali — tum islem try-catch icinde.
    #pragma warning disable CA1031 // Do not catch general exception types — ERP failure must not break order flow
    public async Task Handle(
        DomainEventNotification<InvoiceGeneratedForERPEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        _logger.LogInformation(
            "[ErpInvoiceCreation] Event received: InvoiceId={InvoiceId}, InvoiceNumber={InvoiceNumber}, " +
            "TargetERP={TargetERP}, Amount={Amount}",
            e.InvoiceId, e.InvoiceNumber, e.TargetERP, e.TotalAmount);

        try
        {
            // 1. Resolve ERP provider from TargetERP string
            if (!Enum.TryParse<ErpProvider>(e.TargetERP, ignoreCase: true, out var erpProvider)
                || erpProvider == ErpProvider.None)
            {
                _logger.LogWarning(
                    "[ErpInvoiceCreation] Unknown or None ERP provider '{TargetERP}' for InvoiceId={InvoiceId} — skipping",
                    e.TargetERP, e.InvoiceId);
                return;
            }

            // 2. Check if provider is supported
            if (!_erpAdapterFactory.SupportedProviders.Contains(erpProvider))
            {
                _logger.LogWarning(
                    "[ErpInvoiceCreation] ERP provider '{Provider}' not registered — skipping InvoiceId={InvoiceId}",
                    erpProvider, e.InvoiceId);
                return;
            }

            // 3. Get adapter
            var adapter = _erpAdapterFactory.GetAdapter(erpProvider);

            // 4. Fetch invoice from repository for order reference
            var invoice = await _invoiceRepo.GetByIdAsync(e.InvoiceId).ConfigureAwait(false);
            if (invoice is null)
            {
                _logger.LogWarning(
                    "[ErpInvoiceCreation] Invoice not found: {InvoiceId} — skipping ERP sync", e.InvoiceId);
                return;
            }

            // 5. Fetch order for customer + line item data
            var order = await _orderRepo.GetByIdAsync(invoice.OrderId).ConfigureAwait(false);

            // 6. Create sync log entry
            var syncLog = ErpSyncLog.Create(
                invoice.TenantId,
                erpProvider,
                entityType: "Invoice",
                entityId: e.InvoiceId);

            // 7. If adapter supports account management, ensure customer account exists
            if (adapter is IErpAccountCapable accountCapable && order is not null)
            {
                await EnsureCustomerAccountAsync(
                    accountCapable, order, erpProvider, cancellationToken).ConfigureAwait(false);
            }

            // 8. If adapter supports invoice creation, create the invoice
            if (adapter is IErpInvoiceCapable invoiceCapable)
            {
                var invoiceRequest = BuildInvoiceRequest(order, e);
                var result = await invoiceCapable.CreateInvoiceAsync(
                    invoiceRequest, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    syncLog.MarkSuccess(result.ErpRef ?? result.InvoiceNumber ?? "OK");

                    _logger.LogInformation(
                        "[ErpInvoiceCreation] Invoice created in {Provider}: ErpRef={ErpRef}, " +
                        "InvoiceNumber={ErpInvoiceNumber}, InvoiceId={InvoiceId}",
                        erpProvider, result.ErpRef, result.InvoiceNumber, e.InvoiceId);
                }
                else
                {
                    syncLog.MarkFailure(result.ErrorMessage ?? "Unknown ERP error");

                    _logger.LogError(
                        "[ErpInvoiceCreation] Invoice creation failed in {Provider}: Error={Error}, InvoiceId={InvoiceId}",
                        erpProvider, result.ErrorMessage, e.InvoiceId);
                }
            }
            else
            {
                // Adapter does not support invoice creation — fall back to generic SyncInvoiceAsync
                _logger.LogInformation(
                    "[ErpInvoiceCreation] Adapter {Provider} is not IErpInvoiceCapable — using SyncInvoiceAsync fallback",
                    erpProvider);

                var syncResult = await adapter.SyncInvoiceAsync(e.InvoiceId, cancellationToken).ConfigureAwait(false);

                if (syncResult.Success)
                {
                    syncLog.MarkSuccess(syncResult.ErpRef ?? "OK");

                    _logger.LogInformation(
                        "[ErpInvoiceCreation] Invoice synced via fallback in {Provider}: ErpRef={ErpRef}, InvoiceId={InvoiceId}",
                        erpProvider, syncResult.ErpRef, e.InvoiceId);
                }
                else
                {
                    syncLog.MarkFailure(syncResult.ErrorMessage ?? "Unknown sync error");

                    _logger.LogError(
                        "[ErpInvoiceCreation] Invoice sync fallback failed in {Provider}: Error={Error}, InvoiceId={InvoiceId}",
                        erpProvider, syncResult.ErrorMessage, e.InvoiceId);
                }
            }

            // 9. Persist sync log
            await _syncLogRepo.AddAsync(syncLog, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw; // Do not swallow cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ErpInvoiceCreation] Unhandled error for InvoiceId={InvoiceId}, TargetERP={TargetERP} — " +
                "ERP sync failed but order flow continues",
                e.InvoiceId, e.TargetERP);
        }
    }
    #pragma warning restore CA1031

    // ═══════════════════════════════════════════
    // Private helpers
    // ═══════════════════════════════════════════

    /// <summary>
    /// Musteri cari hesabini ERP'de kontrol eder, yoksa olusturur.
    /// Hata durumunda loglar ama islem devam eder (fatura hala olusturulabilir).
    /// </summary>
    private async Task EnsureCustomerAccountAsync(
        IErpAccountCapable accountCapable,
        Domain.Entities.Order order,
        ErpProvider provider,
        CancellationToken ct)
    {
        var accountCode = $"CUST-{order.CustomerId:N}"[..20]; // Max 20 char ERP account code

        try
        {
            var existing = await accountCapable.GetAccountAsync(accountCode, ct).ConfigureAwait(false);

            if (existing is not null && existing.Success)
            {
                _logger.LogDebug(
                    "[ErpInvoiceCreation] Customer account already exists in {Provider}: {AccountCode}",
                    provider, accountCode);
                return;
            }

            var request = new ErpAccountRequest(
                AccountCode: accountCode,
                CompanyName: order.CustomerName ?? "Bilinmeyen Musteri",
                TaxId: null,
                TaxOffice: null,
                Address: null,
                City: null,
                Phone: null,
                Email: order.CustomerEmail);

            var result = await accountCapable.CreateAccountAsync(request, ct).ConfigureAwait(false);

            if (result.Success)
            {
                _logger.LogInformation(
                    "[ErpInvoiceCreation] Customer account created in {Provider}: {AccountCode} ({AccountName})",
                    provider, result.AccountCode, result.AccountName);
            }
            else
            {
                _logger.LogWarning(
                    "[ErpInvoiceCreation] Customer account creation failed in {Provider}: {Error} — " +
                    "continuing with invoice creation",
                    provider, result.ErrorMessage);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "[ErpInvoiceCreation] Error ensuring customer account {AccountCode} in {Provider} — " +
                "continuing with invoice creation",
                accountCode, provider);
        }
    }

    /// <summary>
    /// Fatura verisinden ErpInvoiceRequest DTO olusturur.
    /// Order varsa satir detaylari eklenir, yoksa tek satirlik ozet olusturulur.
    /// </summary>
    private static ErpInvoiceRequest BuildInvoiceRequest(
        Domain.Entities.Order? order,
        InvoiceGeneratedForERPEvent e)
    {
        var customerCode = order is not null
            ? $"CUST-{order.CustomerId:N}"[..20]
            : "CUST-UNKNOWN";

        var customerName = order?.CustomerName ?? "Bilinmeyen Musteri";

        List<ErpInvoiceLineRequest> lines;

        if (order is not null && order.OrderItems.Count > 0)
        {
            lines = order.OrderItems.Select(item => new ErpInvoiceLineRequest(
                ProductCode: item.ProductSKU,
                ProductName: item.ProductName,
                Quantity: item.Quantity,
                UnitPrice: item.UnitPrice,
                TaxRate: (int)item.TaxRate,
                DiscountAmount: null
            )).ToList();
        }
        else
        {
            // Fallback: tek satirlik ozet fatura
            lines = new List<ErpInvoiceLineRequest>
            {
                new(
                    ProductCode: "SUMM",
                    ProductName: $"Fatura {e.InvoiceNumber}",
                    Quantity: 1,
                    UnitPrice: e.TotalAmount,
                    TaxRate: 20, // Default KDV
                    DiscountAmount: null)
            };
        }

        var subTotal = order?.SubTotal ?? e.TotalAmount;
        var taxTotal = order?.TaxAmount ?? 0m;
        var grandTotal = e.TotalAmount;

        return new ErpInvoiceRequest(
            CustomerCode: customerCode,
            CustomerName: customerName,
            TaxId: null,
            Lines: lines,
            SubTotal: subTotal,
            TaxTotal: taxTotal,
            GrandTotal: grandTotal,
            Currency: "TRY",
            Notes: $"Otomatik ERP fatura — Kaynak: {e.InvoiceNumber}");
    }
}
