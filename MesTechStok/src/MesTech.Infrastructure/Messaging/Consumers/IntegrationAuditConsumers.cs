using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Consumers;

/// <summary>
/// D3-050 FIX: Audit consumers for orphan integration events.
/// These events were published to RabbitMQ but had no consumer —
/// messages accumulated in unbound exchanges or were silently dropped.
///
/// Pattern: log + monitor (same as MesaOutboundAuditConsumers).
/// Business logic consumers can be added later per event.
/// </summary>

// ═══ Core Commerce Events ═══

public sealed class StockChangedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<StockChangedAuditConsumer> logger)
    : IConsumer<StockChangedIntegrationEvent>
{
    public Task Consume(ConsumeContext<StockChangedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] StockChanged: SKU={SKU} Qty={Qty} Source={Source} Tenant={TenantId}",
            m.SKU, m.NewQuantity, m.Source, m.TenantId);
        monitor.RecordConsume("integration.stock.changed");
        return Task.CompletedTask;
    }
}

public sealed class PriceChangedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<PriceChangedAuditConsumer> logger)
    : IConsumer<PriceChangedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PriceChangedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] PriceChanged: SKU={SKU} Price={Price:N2} Source={Source} Tenant={TenantId}",
            m.SKU, m.NewPrice, m.Source, m.TenantId);
        monitor.RecordConsume("integration.price.changed");
        return Task.CompletedTask;
    }
}

public sealed class OrderReceivedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<OrderReceivedAuditConsumer> logger)
    : IConsumer<OrderReceivedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderReceivedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] OrderReceived: Platform={Platform} Order={OrderId} Amount={Amount:N2} Tenant={TenantId}",
            m.PlatformCode, m.PlatformOrderId, m.TotalAmount, m.TenantId);
        monitor.RecordConsume("integration.order.received");
        return Task.CompletedTask;
    }
}

public sealed class InvoiceCreatedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<InvoiceCreatedAuditConsumer> logger)
    : IConsumer<InvoiceCreatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<InvoiceCreatedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] InvoiceCreated: Invoice={InvoiceNumber} Amount={Total:N2} Tenant={TenantId}",
            m.InvoiceNumber, m.GrandTotal, m.TenantId);
        monitor.RecordConsume("integration.invoice.created");
        return Task.CompletedTask;
    }
}

public sealed class OrderShippedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<OrderShippedAuditConsumer> logger)
    : IConsumer<OrderShippedIntegrationEvent>
{
    public Task Consume(ConsumeContext<OrderShippedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] OrderShipped: Order={OrderId} Tracking={Tracking} Cargo={Cargo} Tenant={TenantId}",
            m.OrderId, m.TrackingNumber, m.CargoProvider, m.TenantId);
        monitor.RecordConsume("integration.order.shipped");
        return Task.CompletedTask;
    }
}

public sealed class ProductUpdatedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<ProductUpdatedAuditConsumer> logger)
    : IConsumer<ProductUpdatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ProductUpdatedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] ProductUpdated: SKU={SKU} Field={Field} Tenant={TenantId}",
            m.SKU, m.UpdatedField, m.TenantId);
        monitor.RecordConsume("integration.product.updated");
        return Task.CompletedTask;
    }
}

// ═══ V4 Chain Events ═══

public sealed class ShipmentCostRecordedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<ShipmentCostRecordedAuditConsumer> logger)
    : IConsumer<ShipmentCostRecordedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ShipmentCostRecordedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] ShipmentCostRecorded: Order={OrderId} Cost={Cost:N2} Cargo={Cargo} Tenant={TenantId}",
            m.OrderId, m.ShippingCost, m.CargoProvider, m.TenantId);
        monitor.RecordConsume("integration.shipment.cost");
        return Task.CompletedTask;
    }
}

public sealed class ZeroStockAuditConsumer(
    IMesaEventMonitor monitor, ILogger<ZeroStockAuditConsumer> logger)
    : IConsumer<ZeroStockIntegrationEvent>
{
    public Task Consume(ConsumeContext<ZeroStockIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogWarning("[Audit] ZeroStock: SKU={SKU} PreviousStock={Prev} Tenant={TenantId}",
            m.SKU, m.PreviousStock, m.TenantId);
        monitor.RecordConsume("integration.zero.stock");
        return Task.CompletedTask;
    }
}

public sealed class StaleOrderDetectedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<StaleOrderDetectedAuditConsumer> logger)
    : IConsumer<StaleOrderDetectedIntegrationEvent>
{
    public Task Consume(ConsumeContext<StaleOrderDetectedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogWarning("[Audit] StaleOrderDetected: Order={OrderNumber} Elapsed={Hours:F1}h Tenant={TenantId}",
            m.OrderNumber, m.HoursElapsed, m.TenantId);
        monitor.RecordConsume("integration.stale.order");
        return Task.CompletedTask;
    }
}

public sealed class PlatformDeactivatedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<PlatformDeactivatedAuditConsumer> logger)
    : IConsumer<PlatformDeactivatedIntegrationEvent>
{
    public Task Consume(ConsumeContext<PlatformDeactivatedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogWarning("[Audit] PlatformDeactivated: SKU={SKU} Platform={Platform} Tenant={TenantId}",
            m.SKU, m.PlatformCode, m.TenantId);
        monitor.RecordConsume("integration.platform.deactivated");
        return Task.CompletedTask;
    }
}

// ═══ Dalga 9 Events ═══

public sealed class EInvoiceSentAuditConsumer(
    IMesaEventMonitor monitor, ILogger<EInvoiceSentAuditConsumer> logger)
    : IConsumer<EInvoiceSentIntegrationEvent>
{
    public Task Consume(ConsumeContext<EInvoiceSentIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] EInvoiceSent: ETTN={Ettn} Amount={Amount:N2} Provider={Provider} Tenant={TenantId}",
            m.EttnNo, m.TotalAmount, m.ProviderId, m.TenantId);
        monitor.RecordConsume("integration.einvoice.sent");
        return Task.CompletedTask;
    }
}

public sealed class EInvoiceCancelledAuditConsumer(
    IMesaEventMonitor monitor, ILogger<EInvoiceCancelledAuditConsumer> logger)
    : IConsumer<EInvoiceCancelledIntegrationEvent>
{
    public Task Consume(ConsumeContext<EInvoiceCancelledIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogWarning("[Audit] EInvoiceCancelled: ETTN={Ettn} Reason={Reason} Tenant={TenantId}",
            m.EttnNo, m.Reason, m.TenantId);
        monitor.RecordConsume("integration.einvoice.cancelled");
        return Task.CompletedTask;
    }
}

public sealed class ErpSyncCompletedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<ErpSyncCompletedAuditConsumer> logger)
    : IConsumer<ErpSyncCompletedIntegrationEvent>
{
    public Task Consume(ConsumeContext<ErpSyncCompletedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] ErpSyncCompleted: Provider={Provider} Entity={Entity} Success={Success} Tenant={TenantId}",
            m.ErpProvider, m.EntityType, m.Success, m.TenantId);
        monitor.RecordConsume("integration.erp.sync.completed");
        return Task.CompletedTask;
    }
}

public sealed class EbayOrderReceivedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<EbayOrderReceivedAuditConsumer> logger)
    : IConsumer<EbayOrderReceivedIntegrationEvent>
{
    public Task Consume(ConsumeContext<EbayOrderReceivedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] EbayOrderReceived: OrderId={OrderId} Buyer={Buyer} Amount={Amount:N2} Tenant={TenantId}",
            m.EbayOrderId, m.BuyerUsername, m.TotalAmount, m.TenantId);
        monitor.RecordConsume("integration.ebay.order");
        return Task.CompletedTask;
    }
}

public sealed class CreditBalanceLowAuditConsumer(
    IMesaEventMonitor monitor, ILogger<CreditBalanceLowAuditConsumer> logger)
    : IConsumer<CreditBalanceLowIntegrationEvent>
{
    public Task Consume(ConsumeContext<CreditBalanceLowIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogWarning("[Audit] CreditBalanceLow: Provider={Provider} Remaining={Remaining}/{Threshold} Tenant={TenantId}",
            m.ProviderId, m.RemainingCredits, m.ThresholdCredits, m.TenantId);
        monitor.RecordConsume("integration.credit.balance.low");
        return Task.CompletedTask;
    }
}

// ═══ CRM Events ═══

public sealed class LeadConvertedAuditConsumer(
    IMesaEventMonitor monitor, ILogger<LeadConvertedAuditConsumer> logger)
    : IConsumer<LeadConvertedIntegrationEvent>
{
    public Task Consume(ConsumeContext<LeadConvertedIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] LeadConverted: Lead={LeadId} → Contact={ContactId} Name={Name} Tenant={TenantId}",
            m.LeadId, m.CrmContactId, m.FullName, m.TenantId);
        monitor.RecordConsume("integration.lead.converted");
        return Task.CompletedTask;
    }
}

public sealed class DealWonAuditConsumer(
    IMesaEventMonitor monitor, ILogger<DealWonAuditConsumer> logger)
    : IConsumer<DealWonIntegrationEvent>
{
    public Task Consume(ConsumeContext<DealWonIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogInformation("[Audit] DealWon: Deal={DealId} Title={Title} Amount={Amount:N2} Tenant={TenantId}",
            m.DealId, m.DealTitle, m.Amount, m.TenantId);
        monitor.RecordConsume("integration.deal.won");
        return Task.CompletedTask;
    }
}

public sealed class DealLostAuditConsumer(
    IMesaEventMonitor monitor, ILogger<DealLostAuditConsumer> logger)
    : IConsumer<DealLostIntegrationEvent>
{
    public Task Consume(ConsumeContext<DealLostIntegrationEvent> context)
    {
        var m = context.Message;
        logger.LogWarning("[Audit] DealLost: Deal={DealId} Title={Title} Reason={Reason} Tenant={TenantId}",
            m.DealId, m.DealTitle, m.Reason, m.TenantId);
        monitor.RecordConsume("integration.deal.lost");
        return Task.CompletedTask;
    }
}
