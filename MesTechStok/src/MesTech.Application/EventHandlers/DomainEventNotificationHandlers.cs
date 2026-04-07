using Microsoft.Extensions.Logging;

namespace MesTech.Application.EventHandlers;

/// <summary>
/// Toplu domain event notification handler'lari — orphan event'ler icin audit log.
/// Her handler sadece structured log yazar — gelecekte bildirim servisi baglanir.
/// </summary>

// ── Settlement ──
public interface ISettlementDisputedNotificationHandler
{
    Task HandleAsync(Guid batchId, Guid tenantId, string platform, decimal totalNet, CancellationToken ct);
}

public sealed class SettlementDisputedNotificationHandler : ISettlementDisputedNotificationHandler
{
    private readonly ILogger<SettlementDisputedNotificationHandler> _logger;
    public SettlementDisputedNotificationHandler(ILogger<SettlementDisputedNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid batchId, Guid tenantId, string platform, decimal totalNet, CancellationToken ct)
    {
        _logger.LogWarning(
            "Settlement DISPUTED — BatchId={BatchId}, Platform={Platform}, Net={Net:F2}, TenantId={TenantId}",
            batchId, platform, totalNet, tenantId);
        return Task.CompletedTask;
    }
}

// ── Finance ──
public interface IBankBalanceChangedNotificationHandler
{
    Task HandleAsync(Guid bankAccountId, Guid tenantId, decimal newBalance, CancellationToken ct);
}

public sealed class BankBalanceChangedNotificationHandler : IBankBalanceChangedNotificationHandler
{
    private readonly ILogger<BankBalanceChangedNotificationHandler> _logger;
    public BankBalanceChangedNotificationHandler(ILogger<BankBalanceChangedNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid bankAccountId, Guid tenantId, decimal newBalance, CancellationToken ct)
    {
        _logger.LogInformation(
            "BankBalance CHANGED — AccountId={AccountId}, NewBalance={Balance:F2}, TenantId={TenantId}",
            bankAccountId, newBalance, tenantId);
        return Task.CompletedTask;
    }
}

// ── CRM ──
public interface ICariHesapCreatedNotificationHandler
{
    Task HandleAsync(Guid cariHesapId, Guid tenantId, string name, CancellationToken ct);
}

public sealed class CariHesapCreatedNotificationHandler : ICariHesapCreatedNotificationHandler
{
    private readonly ILogger<CariHesapCreatedNotificationHandler> _logger;
    public CariHesapCreatedNotificationHandler(ILogger<CariHesapCreatedNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid cariHesapId, Guid tenantId, string name, CancellationToken ct)
    {
        _logger.LogInformation("CariHesap CREATED — Id={Id}, Name={Name}, TenantId={TenantId}",
            cariHesapId, name, tenantId);
        return Task.CompletedTask;
    }
}

public interface ICariHareketRecordedNotificationHandler
{
    Task HandleAsync(Guid cariHareketId, Guid tenantId, decimal amount, CancellationToken ct);
}

public sealed class CariHareketRecordedNotificationHandler : ICariHareketRecordedNotificationHandler
{
    private readonly ILogger<CariHareketRecordedNotificationHandler> _logger;
    public CariHareketRecordedNotificationHandler(ILogger<CariHareketRecordedNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid cariHareketId, Guid tenantId, decimal amount, CancellationToken ct)
    {
        _logger.LogInformation("CariHareket RECORDED — Id={Id}, Amount={Amount:F2}, TenantId={TenantId}",
            cariHareketId, amount, tenantId);
        return Task.CompletedTask;
    }
}

// ── Expense: Submitted/Rejected mevcut IExpenseNotificationHandler'a extend edilecek ──

// ── Invoice ──
public interface IInvoicePlatformSentNotificationHandler
{
    Task HandleAsync(Guid invoiceId, Guid tenantId, string platformUrl, CancellationToken ct);
}

public sealed class InvoicePlatformSentNotificationHandler : IInvoicePlatformSentNotificationHandler
{
    private readonly ILogger<InvoicePlatformSentNotificationHandler> _logger;
    public InvoicePlatformSentNotificationHandler(ILogger<InvoicePlatformSentNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid invoiceId, Guid tenantId, string platformUrl, CancellationToken ct)
    {
        _logger.LogInformation("Invoice PLATFORM_SENT — Id={Id}, Url={Url}, TenantId={TenantId}",
            invoiceId, platformUrl, tenantId);
        return Task.CompletedTask;
    }
}

// ── Quotation ──
public interface IQuotationNotificationHandler
{
    Task HandleAcceptedAsync(Guid quotationId, Guid tenantId, CancellationToken ct);
    Task HandleRejectedAsync(Guid quotationId, Guid tenantId, CancellationToken ct);
    Task HandleConvertedAsync(Guid quotationId, Guid tenantId, Guid orderId, CancellationToken ct);
}

public sealed class QuotationNotificationHandlerImpl : IQuotationNotificationHandler
{
    private readonly ILogger<QuotationNotificationHandlerImpl> _logger;
    public QuotationNotificationHandlerImpl(ILogger<QuotationNotificationHandlerImpl> logger) => _logger = logger;

    public Task HandleAcceptedAsync(Guid quotationId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogInformation("Quotation ACCEPTED — Id={Id}, TenantId={TenantId}", quotationId, tenantId);
        return Task.CompletedTask;
    }

    public Task HandleRejectedAsync(Guid quotationId, Guid tenantId, CancellationToken ct)
    {
        _logger.LogWarning("Quotation REJECTED — Id={Id}, TenantId={TenantId}", quotationId, tenantId);
        return Task.CompletedTask;
    }

    public Task HandleConvertedAsync(Guid quotationId, Guid tenantId, Guid orderId, CancellationToken ct)
    {
        _logger.LogInformation("Quotation CONVERTED to Order — QuotationId={QId}, OrderId={OId}, TenantId={TenantId}",
            quotationId, orderId, tenantId);
        return Task.CompletedTask;
    }
}

// ── Return ──
public interface IReturnRejectedNotificationHandler
{
    Task HandleAsync(Guid returnId, Guid tenantId, string? reason, CancellationToken ct);
}

public sealed class ReturnRejectedNotificationHandler : IReturnRejectedNotificationHandler
{
    private readonly ILogger<ReturnRejectedNotificationHandler> _logger;
    public ReturnRejectedNotificationHandler(ILogger<ReturnRejectedNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid returnId, Guid tenantId, string? reason, CancellationToken ct)
    {
        _logger.LogWarning("Return REJECTED — Id={Id}, Reason={Reason}, TenantId={TenantId}",
            returnId, reason, tenantId);
        return Task.CompletedTask;
    }
}

// ── Campaign ──
public interface ICampaignCreatedNotificationHandler
{
    Task HandleAsync(Guid campaignId, Guid tenantId, string name, CancellationToken ct);
}

public sealed class CampaignCreatedNotificationHandler : ICampaignCreatedNotificationHandler
{
    private readonly ILogger<CampaignCreatedNotificationHandler> _logger;
    public CampaignCreatedNotificationHandler(ILogger<CampaignCreatedNotificationHandler> logger) => _logger = logger;

    public Task HandleAsync(Guid campaignId, Guid tenantId, string name, CancellationToken ct)
    {
        _logger.LogInformation("Campaign CREATED — Id={Id}, Name={Name}, TenantId={TenantId}",
            campaignId, name, tenantId);
        return Task.CompletedTask;
    }
}
