using MassTransit;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Consumers;

/// <summary>
/// 8 OUTBOUND Finance event icin ic audit consumer.
/// Bu event'ler MesTech → MESA OS yonunde publish edilir.
/// Consumer'lar loopback olarak ic monitoring + Seq audit trail saglar.
/// DEV3 TUR1: G703 orphan Finance event kapatma.
/// </summary>

public sealed class FinanceAnomalyDetectedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceAnomalyDetectedAuditConsumer> logger) : IConsumer<FinanceAnomalyDetectedEvent>
{
    public Task Consume(ConsumeContext<FinanceAnomalyDetectedEvent> context)
    {
        var msg = context.Message;
        logger.LogWarning(
            "[MESA Audit] FinanceAnomalyDetected: Type={AnomalyType} Expected={Expected} Actual={Actual} Entity={EntityType}:{EntityId} TenantId={TenantId}",
            msg.AnomalyType, msg.ExpectedAmount, msg.ActualAmount, msg.EntityType, msg.EntityId, msg.TenantId);
        monitor.RecordConsume("finance.anomaly.detected");
        return Task.CompletedTask;
    }
}

public sealed class FinanceBankImportedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceBankImportedAuditConsumer> logger) : IConsumer<FinanceBankImportedEvent>
{
    public Task Consume(ConsumeContext<FinanceBankImportedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceBankImported: BankAccountId={BankAccountId} TxCount={TxCount} Credits={Credits} Debits={Debits} Date={ImportDate} TenantId={TenantId}",
            msg.BankAccountId, msg.TransactionCount, msg.TotalCredits, msg.TotalDebits, msg.ImportDate, msg.TenantId);
        monitor.RecordConsume("finance.bank.imported");
        return Task.CompletedTask;
    }
}

public sealed class FinanceDocumentReceivedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceDocumentReceivedAuditConsumer> logger) : IConsumer<FinanceDocumentReceivedEvent>
{
    public Task Consume(ConsumeContext<FinanceDocumentReceivedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceDocumentReceived: DocumentId={DocumentId} FileName={FileName} MimeType={MimeType} Source={Source} TenantId={TenantId}",
            msg.DocumentId, msg.FileName, msg.MimeType, msg.Source, msg.TenantId);
        monitor.RecordConsume("finance.document.received");
        return Task.CompletedTask;
    }
}

public sealed class FinanceLedgerPostedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceLedgerPostedAuditConsumer> logger) : IConsumer<FinanceLedgerPostedEvent>
{
    public Task Consume(ConsumeContext<FinanceLedgerPostedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceLedgerPosted: JournalEntryId={JournalEntryId} Amount={Amount} Source={Source} Accounts={Accounts} TenantId={TenantId}",
            msg.JournalEntryId, msg.TotalAmount, msg.Source, string.Join(",", msg.AccountCodes), msg.TenantId);
        monitor.RecordConsume("finance.ledger.posted");
        return Task.CompletedTask;
    }
}

public sealed class FinanceReconciliationPendingAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceReconciliationPendingAuditConsumer> logger) : IConsumer<FinanceReconciliationPendingEvent>
{
    public Task Consume(ConsumeContext<FinanceReconciliationPendingEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceReconciliationPending: MatchId={MatchId} Confidence={Confidence:P0} SettlementAmt={SettlementAmt} BankTxAmt={BankTxAmt} TenantId={TenantId}",
            msg.MatchId, msg.Confidence, msg.SettlementAmount, msg.BankTxAmount, msg.TenantId);
        monitor.RecordConsume("finance.reconciliation.pending");
        return Task.CompletedTask;
    }
}

public sealed class FinanceReportDailyAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceReportDailyAuditConsumer> logger) : IConsumer<FinanceReportDailyEvent>
{
    public Task Consume(ConsumeContext<FinanceReportDailyEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceReportDaily: Date={Date} Orders={Orders} Revenue={Revenue} Commission={Commission} Cargo={Cargo} NetProfit={NetProfit} TenantId={TenantId}",
            msg.Date, msg.OrderCount, msg.TotalRevenue, msg.TotalCommission, msg.TotalCargo, msg.NetProfit, msg.TenantId);
        monitor.RecordConsume("finance.report.daily");
        return Task.CompletedTask;
    }
}

public sealed class FinanceSettlementImportedAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceSettlementImportedAuditConsumer> logger) : IConsumer<FinanceSettlementImportedEvent>
{
    public Task Consume(ConsumeContext<FinanceSettlementImportedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceSettlementImported: BatchId={BatchId} Platform={Platform} Period={Start}~{End} Net={Net} Lines={Lines} TenantId={TenantId}",
            msg.SettlementBatchId, msg.Platform, msg.PeriodStart, msg.PeriodEnd, msg.TotalNet, msg.LineCount, msg.TenantId);
        monitor.RecordConsume("finance.settlement.imported");
        return Task.CompletedTask;
    }
}

public sealed class FinanceTaxPrepReadyAuditConsumer(
    IMesaEventMonitor monitor,
    ILogger<FinanceTaxPrepReadyAuditConsumer> logger) : IConsumer<FinanceTaxPrepReadyEvent>
{
    public Task Consume(ConsumeContext<FinanceTaxPrepReadyEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "[MESA Audit] FinanceTaxPrepReady: Period={Year}/{Month} Sales={Sales} Purchases={Purchases} PayableVAT={PayableVAT} Withholding={Withholding} TenantId={TenantId}",
            msg.Year, msg.Month, msg.TotalSales, msg.TotalPurchases, msg.PayableVAT, msg.TotalWithholding, msg.TenantId);
        monitor.RecordConsume("finance.tax-prep.ready");
        return Task.CompletedTask;
    }
}
