using MassTransit;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Consumers;

/// <summary>
/// 12 OUTBOUND MESA event için iç audit consumer.
/// Bu event'ler MesTech → MESA OS yönünde publish edilir.
/// Consumer'lar loopback olarak iç monitoring + Seq audit trail sağlar.
/// DEV6 TUR15: G515 orphan event kapatma.
/// </summary>

public sealed class MesaProductCreatedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaProductCreatedAuditConsumer> logger) : IConsumer<MesaProductCreatedEvent>
{
    public Task Consume(ConsumeContext<MesaProductCreatedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] ProductCreated: SKU={SKU} Name={Name} Price={Price} TenantId={TenantId}",
            msg.SKU, msg.Name, msg.SalePrice, msg.TenantId);
        monitor.RecordConsume("mesa.product.created");
        return Task.CompletedTask;
    }
}

public sealed class MesaStockLowAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaStockLowAuditConsumer> logger) : IConsumer<MesaStockLowEvent>
{
    public Task Consume(ConsumeContext<MesaStockLowEvent> context)
    {
        var msg = context.Message;
        logger.LogWarning(
            "[MESA Audit] StockLow: SKU={SKU} Current={Current} Min={Min} TenantId={TenantId}",
            msg.SKU, msg.CurrentStock, msg.MinimumStock, msg.TenantId);
        monitor.RecordConsume("mesa.stock.low");
        return Task.CompletedTask;
    }
}

public sealed class MesaOrderReceivedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaOrderReceivedAuditConsumer> logger) : IConsumer<MesaOrderReceivedEvent>
{
    public Task Consume(ConsumeContext<MesaOrderReceivedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] OrderReceived: OrderId={OrderId} Platform={Platform} Amount={Amount} TenantId={TenantId}",
            msg.OrderId, msg.PlatformCode, msg.TotalAmount, msg.TenantId);
        monitor.RecordConsume("mesa.order.received");
        return Task.CompletedTask;
    }
}

public sealed class MesaPriceChangedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaPriceChangedAuditConsumer> logger) : IConsumer<MesaPriceChangedEvent>
{
    public Task Consume(ConsumeContext<MesaPriceChangedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] PriceChanged: SKU={SKU} Old={Old} New={New} TenantId={TenantId}",
            msg.SKU, msg.OldPrice, msg.NewPrice, msg.TenantId);
        monitor.RecordConsume("mesa.price.changed");
        return Task.CompletedTask;
    }
}

public sealed class MesaInvoiceGeneratedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaInvoiceGeneratedAuditConsumer> logger) : IConsumer<MesaInvoiceGeneratedEvent>
{
    public Task Consume(ConsumeContext<MesaInvoiceGeneratedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] InvoiceGenerated: InvoiceNo={InvoiceNo} OrderId={OrderId} Total={Total} TenantId={TenantId}",
            msg.InvoiceNumber, msg.OrderId, msg.GrandTotal, msg.TenantId);
        monitor.RecordConsume("mesa.invoice.generated");
        return Task.CompletedTask;
    }
}

public sealed class MesaInvoiceCancelledAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaInvoiceCancelledAuditConsumer> logger) : IConsumer<MesaInvoiceCancelledEvent>
{
    public Task Consume(ConsumeContext<MesaInvoiceCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogWarning(
            "[MESA Audit] InvoiceCancelled: InvoiceNo={InvoiceNo} Reason={Reason} TenantId={TenantId}",
            msg.InvoiceNumber, msg.CancelReason, msg.TenantId);
        monitor.RecordConsume("mesa.invoice.cancelled");
        return Task.CompletedTask;
    }
}

public sealed class MesaReturnCreatedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaReturnCreatedAuditConsumer> logger) : IConsumer<MesaReturnCreatedEvent>
{
    public Task Consume(ConsumeContext<MesaReturnCreatedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] ReturnCreated: ReturnId={ReturnId} OrderId={OrderId} Items={Items} Amount={Amount} TenantId={TenantId}",
            msg.ReturnRequestId, msg.OrderId, msg.ItemCount, msg.TotalAmount, msg.TenantId);
        monitor.RecordConsume("mesa.return.created");
        return Task.CompletedTask;
    }
}

public sealed class MesaReturnResolvedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaReturnResolvedAuditConsumer> logger) : IConsumer<MesaReturnResolvedEvent>
{
    public Task Consume(ConsumeContext<MesaReturnResolvedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] ReturnResolved: ReturnId={ReturnId} Resolution={Resolution} Refund={Refund} TenantId={TenantId}",
            msg.ReturnRequestId, msg.Resolution, msg.RefundAmount, msg.TenantId);
        monitor.RecordConsume("mesa.return.resolved");
        return Task.CompletedTask;
    }
}

public sealed class MesaBuyboxLostAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaBuyboxLostAuditConsumer> logger) : IConsumer<MesaBuyboxLostEvent>
{
    public Task Consume(ConsumeContext<MesaBuyboxLostEvent> context)
    {
        var msg = context.Message;
        logger.LogWarning(
            "[MESA Audit] BuyboxLost: SKU={SKU} OurPrice={OurPrice} CompetitorPrice={CompPrice} Diff={Diff} TenantId={TenantId}",
            msg.SKU, msg.CurrentPrice, msg.CompetitorPrice, msg.PriceDifference, msg.TenantId);
        monitor.RecordConsume("mesa.buybox.lost");
        return Task.CompletedTask;
    }
}

public sealed class MesaSupplierFeedSyncedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaSupplierFeedSyncedAuditConsumer> logger) : IConsumer<MesaSupplierFeedSyncedEvent>
{
    public Task Consume(ConsumeContext<MesaSupplierFeedSyncedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] SupplierFeedSynced: Supplier={Supplier} Format={Format} Total={Total} New={New} Updated={Updated} TenantId={TenantId}",
            msg.SupplierName, msg.FeedFormat, msg.ProductsTotal, msg.ProductsNew, msg.ProductsUpdated, msg.TenantId);
        monitor.RecordConsume("mesa.supplier.feed.synced");
        return Task.CompletedTask;
    }
}

public sealed class MesaDailySummaryAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaDailySummaryAuditConsumer> logger) : IConsumer<MesaDailySummaryEvent>
{
    public Task Consume(ConsumeContext<MesaDailySummaryEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] DailySummary: Date={Date} Orders={Orders} Revenue={Revenue} Alerts={Alerts} Invoices={Invoices} TenantId={TenantId}",
            msg.Date, msg.OrderCount, msg.Revenue, msg.StockAlerts, msg.InvoiceCount, msg.TenantId);
        monitor.RecordConsume("mesa.daily.summary");
        return Task.CompletedTask;
    }
}

public sealed class MesaSyncErrorAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<MesaSyncErrorAuditConsumer> logger) : IConsumer<MesaSyncErrorEvent>
{
    public Task Consume(ConsumeContext<MesaSyncErrorEvent> context)
    {
        var msg = context.Message;
        logger.LogError(
            "[MESA Audit] SyncError: Platform={Platform} Type={ErrorType} Message={Message} TenantId={TenantId}",
            msg.Platform, msg.ErrorType, msg.Message, msg.TenantId);
        monitor.RecordConsume("mesa.sync.error");
        return Task.CompletedTask;
    }
}
