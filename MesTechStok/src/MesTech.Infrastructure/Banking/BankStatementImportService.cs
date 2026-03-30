using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Banking;

/// <summary>
/// Banka ekstre import orkestratoru.
/// Format tespit → parse → idempotency key ile dedup → kaydet → event yayinla.
/// </summary>
public sealed class BankStatementImportService
{
    private readonly IBankStatementParserFactory _parserFactory;
    private readonly IBankTransactionRepository _transactionRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BankStatementImportService> _logger;

    public BankStatementImportService(
        IBankStatementParserFactory parserFactory,
        IBankTransactionRepository transactionRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<BankStatementImportService> logger)
    {
        _parserFactory = parserFactory;
        _transactionRepository = transactionRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Banka ekstre dosyasini import eder.
    /// Format otomatik tespit edilir veya belirtilir.
    /// </summary>
    public async Task<BankStatementImportResult> ImportAsync(
        Stream data,
        Guid bankAccountId,
        string? format = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var tenantId = _tenantProvider.GetCurrentTenantId();

        // 1. Format tespit
        var detectedFormat = format ?? _parserFactory.DetectFormat(data);
        _logger.LogInformation(
            "[BankStatementImport] Format: {Format}, BankAccountId: {BankAccountId}, TenantId: {TenantId}",
            detectedFormat, bankAccountId, tenantId);

        // 2. Parse
        var parser = _parserFactory.GetParser(detectedFormat);
        var parsedTransactions = await parser.ParseAsync(data, bankAccountId, ct);

        _logger.LogInformation(
            "[BankStatementImport] {ParsedCount} islem parse edildi", parsedTransactions.Count);

        // 3. Deduplicate — idempotency key kontrolu
        var newTransactions = new List<Domain.Accounting.Entities.BankTransaction>();
        var duplicateCount = 0;

        foreach (var txn in parsedTransactions)
        {
            ct.ThrowIfCancellationRequested();

            if (txn.IdempotencyKey != null)
            {
                var existing = await _transactionRepository.GetByIdempotencyKeyAsync(
                    tenantId, txn.IdempotencyKey, ct);

                if (existing != null)
                {
                    duplicateCount++;
                    continue;
                }
            }

            // Tenant ID'yi set et (parser'lar Guid.Empty ile olusturur)
            var transaction = Domain.Accounting.Entities.BankTransaction.Create(
                tenantId: tenantId,
                bankAccountId: bankAccountId,
                transactionDate: txn.TransactionDate,
                amount: txn.Amount,
                description: txn.Description,
                referenceNumber: txn.ReferenceNumber,
                idempotencyKey: txn.IdempotencyKey);

            newTransactions.Add(transaction);
        }

        _logger.LogInformation(
            "[BankStatementImport] {NewCount} yeni, {DuplicateCount} tekrar islem",
            newTransactions.Count, duplicateCount);

        // 4. Save
        if (newTransactions.Count > 0)
        {
            await _transactionRepository.AddRangeAsync(newTransactions, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        // 5. Calculate totals for event
        var totalInflow = newTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalOutflow = newTransactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));

        _logger.LogInformation(
            "[BankStatementImport] Import tamamlandi — {NewCount} islem kaydedildi, " +
            "Giris: {Inflow:F2}, Cikis: {Outflow:F2}",
            newTransactions.Count, totalInflow, totalOutflow);

        return new BankStatementImportResult
        {
            Format = detectedFormat,
            BankAccountId = bankAccountId,
            TenantId = tenantId,
            TotalParsed = parsedTransactions.Count,
            NewTransactions = newTransactions.Count,
            DuplicateCount = duplicateCount,
            TotalInflow = totalInflow,
            TotalOutflow = totalOutflow,
            DomainEvent = new BankStatementImportedEvent
            {
                TenantId = tenantId,
                BankAccountId = bankAccountId,
                TransactionCount = newTransactions.Count,
                TotalInflow = totalInflow,
                TotalOutflow = totalOutflow,
                CorrelationId = Guid.NewGuid(),
                EventType = nameof(BankStatementImportedEvent)
            }
        };
    }
}

/// <summary>
/// Banka ekstre import sonucu.
/// </summary>
public sealed class BankStatementImportResult
{
    public string Format { get; init; } = string.Empty;
    public Guid BankAccountId { get; init; }
    public Guid TenantId { get; init; }
    public int TotalParsed { get; init; }
    public int NewTransactions { get; init; }
    public int DuplicateCount { get; init; }
    public decimal TotalInflow { get; init; }
    public decimal TotalOutflow { get; init; }
    public BankStatementImportedEvent? DomainEvent { get; init; }
}
