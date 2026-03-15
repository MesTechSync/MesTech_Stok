using Hangfire;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Event-driven ERP sync handler — implements IERPSyncHandler.
/// Fatura olusturuldugunda veya siparis alindigi zaman otomatik ERP push tetikler.
/// IERPAdapterFactory uzerinden aktif tenant ERP'sini resolve eder.
///
/// Failure policy: hata loglanir + Hangfire ile retry job enqueue edilir.
/// </summary>
public sealed class ERPSyncHandler : IERPSyncHandler
{
    private readonly IERPAdapterFactory _erpAdapterFactory;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ERPSyncHandler> _logger;

    public ERPSyncHandler(
        IERPAdapterFactory erpAdapterFactory,
        IInvoiceRepository invoiceRepository,
        IOrderRepository orderRepository,
        IBackgroundJobClient backgroundJobClient,
        ILogger<ERPSyncHandler> logger)
    {
        _erpAdapterFactory = erpAdapterFactory ?? throw new ArgumentNullException(nameof(erpAdapterFactory));
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════
    // IERPSyncHandler implementation
    // ═══════════════════════════════════════════

    /// <summary>
    /// Fatura olusturuldugunda: aktif ERP'ye fatura senkronizasyonu gonder.
    /// IERPAdapterFactory -> SyncInvoicesAsync
    /// </summary>
    public async Task HandleInvoiceCreatedAsync(Guid invoiceId, CancellationToken ct = default)
    {
        _logger.LogInformation("[ERPSync] HandleInvoiceCreated: InvoiceId={InvoiceId}", invoiceId);

        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId).ConfigureAwait(false);
            if (invoice is null)
            {
                _logger.LogWarning("[ERPSync] Invoice not found: {InvoiceId}", invoiceId);
                return;
            }

            // Determine target ERP — use invoice's platform code or fall back to first supported ERP.
            // In a multi-tenant scenario, tenant config would provide the ERP name.
            // Here we use the first available ERP as a safe default.
            var erpName = ResolveERPName(invoice.PlatformCode);
            if (string.IsNullOrWhiteSpace(erpName))
            {
                _logger.LogWarning("[ERPSync] No ERP configured for tenant — skipping sync for InvoiceId={InvoiceId}",
                    invoiceId);
                return;
            }

            var adapter = _erpAdapterFactory.GetAdapter(erpName);
            _logger.LogInformation("[ERPSync] Syncing invoice {InvoiceId} to ERP '{ERPName}'", invoiceId, erpName);

            await adapter.SyncInvoicesAsync(new[] { invoice }, ct).ConfigureAwait(false);

            _logger.LogInformation("[ERPSync] Invoice {InvoiceId} synced successfully to {ERPName}",
                invoiceId, erpName);
        }
        catch (ArgumentException ex)
        {
            // Unsupported ERP — log and skip retry (configuration issue, not transient)
            _logger.LogError(ex, "[ERPSync] Unsupported ERP for invoice {InvoiceId} — no retry", invoiceId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERPSync] HandleInvoiceCreated failed for InvoiceId={InvoiceId} — enqueuing Hangfire retry",
                invoiceId);

            // Enqueue Hangfire retry job (background, automatic retry with backoff)
            _backgroundJobClient.Enqueue<ERPSyncHandler>(
                handler => handler.RetryInvoiceSyncAsync(invoiceId, CancellationToken.None));
        }
    }

    /// <summary>
    /// Siparis alindigi zaman: aktif ERP'ye musteri (counterparty) senkronizasyonu gonder.
    /// IERPAdapterFactory -> SyncCounterpartiesAsync
    /// </summary>
    public async Task HandleOrderReceivedAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("[ERPSync] HandleOrderReceived: OrderId={OrderId}", orderId);

        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
            if (order is null)
            {
                _logger.LogWarning("[ERPSync] Order not found: {OrderId}", orderId);
                return;
            }

            var erpName = ResolveERPName(order.SourcePlatform?.ToString());
            if (string.IsNullOrWhiteSpace(erpName))
            {
                _logger.LogWarning("[ERPSync] No ERP configured for tenant — skipping counterparty sync for OrderId={OrderId}",
                    orderId);
                return;
            }

            var adapter = _erpAdapterFactory.GetAdapter(erpName);
            _logger.LogInformation("[ERPSync] Syncing counterparty for order {OrderId} to ERP '{ERPName}'",
                orderId, erpName);

            // Build counterparty DTO from order customer info
            var counterparty = new CounterpartyDto
            {
                Id = order.Id,
                Name = order.CustomerName ?? string.Empty,
                Email = order.CustomerEmail,
                CounterpartyType = "Customer",
                Platform = order.SourcePlatform?.ToString(),
                IsActive = true
            };

            await adapter.SyncCounterpartiesAsync(new[] { counterparty }, ct).ConfigureAwait(false);

            _logger.LogInformation("[ERPSync] Counterparty for order {OrderId} synced successfully to {ERPName}",
                orderId, erpName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "[ERPSync] Unsupported ERP for order {OrderId} — no retry", orderId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ERPSync] HandleOrderReceived failed for OrderId={OrderId} — enqueuing Hangfire retry",
                orderId);

            _backgroundJobClient.Enqueue<ERPSyncHandler>(
                handler => handler.RetryOrderSyncAsync(orderId, CancellationToken.None));
        }
    }

    // ═══════════════════════════════════════════
    // Hangfire retry targets (public — Hangfire requires public methods)
    // ═══════════════════════════════════════════

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task RetryInvoiceSyncAsync(Guid invoiceId, CancellationToken ct)
    {
        _logger.LogInformation("[ERPSync] Hangfire retry: InvoiceId={InvoiceId}", invoiceId);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId).ConfigureAwait(false);
        if (invoice is null)
        {
            _logger.LogWarning("[ERPSync] Retry: Invoice not found — {InvoiceId}", invoiceId);
            return;
        }

        var erpName = ResolveERPName(invoice.PlatformCode);
        if (string.IsNullOrWhiteSpace(erpName))
            return;

        var adapter = _erpAdapterFactory.GetAdapter(erpName);
        await adapter.SyncInvoicesAsync(new[] { invoice }, ct).ConfigureAwait(false);

        _logger.LogInformation("[ERPSync] Hangfire retry success: InvoiceId={InvoiceId} → {ERP}",
            invoiceId, erpName);
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task RetryOrderSyncAsync(Guid orderId, CancellationToken ct)
    {
        _logger.LogInformation("[ERPSync] Hangfire retry: OrderId={OrderId}", orderId);

        var order = await _orderRepository.GetByIdAsync(orderId).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogWarning("[ERPSync] Retry: Order not found — {OrderId}", orderId);
            return;
        }

        var erpName = ResolveERPName(order.SourcePlatform?.ToString());
        if (string.IsNullOrWhiteSpace(erpName))
            return;

        var adapter = _erpAdapterFactory.GetAdapter(erpName);

        var counterparty = new CounterpartyDto
        {
            Id = order.Id,
            Name = order.CustomerName ?? string.Empty,
            Email = order.CustomerEmail,
            CounterpartyType = "Customer",
            Platform = order.SourcePlatform?.ToString(),
            IsActive = true
        };

        await adapter.SyncCounterpartiesAsync(new[] { counterparty }, ct).ConfigureAwait(false);

        _logger.LogInformation("[ERPSync] Hangfire retry success: OrderId={OrderId} → {ERP}",
            orderId, erpName);
    }

    // ═══════════════════════════════════════════
    // Private helpers
    // ═══════════════════════════════════════════

    /// <summary>
    /// Resolves the ERP name for the current tenant.
    /// Currently returns the first supported ERP as a placeholder.
    /// TODO: In multi-tenant scenario, read from TenantConfiguration (DEV 1 task 1.08).
    /// </summary>
    private string? ResolveERPName(string? platformCode)
    {
        // Multi-tenant: tenant config provides ERP name.
        // For now, return first available ERP from factory.
        var supported = _erpAdapterFactory.SupportedERPs;
        if (supported.Count == 0)
        {
            _logger.LogWarning("[ERPSync] No ERP adapters registered in factory");
            return null;
        }

        // Default: first registered ERP (Parasut in production)
        return supported[0];
    }
}
