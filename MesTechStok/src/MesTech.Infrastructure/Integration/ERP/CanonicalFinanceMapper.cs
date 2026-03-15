using System.Globalization;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;

using InvoiceEntity = MesTech.Domain.Entities.Invoice;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Maps MesTech domain entities to a canonical format for ERP sync.
/// Provides normalization, validation, and transformation utilities
/// shared across all ERP adapters.
/// </summary>
public sealed class CanonicalFinanceMapper
{
    /// <summary>
    /// Maps a list of invoices to a normalized format suitable for ERP sync.
    /// Filters out cancelled/rejected invoices, validates required fields.
    /// </summary>
    public IReadOnlyList<InvoiceEntity> NormalizeInvoices(IEnumerable<InvoiceEntity> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        return invoices
            .Where(i => i.Status != InvoiceStatus.Cancelled
                     && i.Status != InvoiceStatus.Rejected)
            .Where(i => i.GrandTotal != 0m)
            .Where(i => !string.IsNullOrWhiteSpace(i.InvoiceNumber))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Maps a list of expenses to a normalized format suitable for ERP sync.
    /// Filters out unapproved expenses, validates required fields.
    /// </summary>
    public IReadOnlyList<AccountingExpenseDto> NormalizeExpenses(IEnumerable<AccountingExpenseDto> expenses)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        return expenses
            .Where(e => e.IsApproved)
            .Where(e => e.Amount != 0m)
            .Where(e => !string.IsNullOrWhiteSpace(e.Title))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Maps a list of counterparties to a normalized format suitable for ERP sync.
    /// Validates required fields, deduplicates by VKN.
    /// </summary>
    public IReadOnlyList<CounterpartyDto> NormalizeCounterparties(IEnumerable<CounterpartyDto> parties)
    {
        ArgumentNullException.ThrowIfNull(parties);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return parties
            .Where(p => p.IsActive)
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Where(p =>
            {
                // Deduplicate by VKN if present
                if (string.IsNullOrEmpty(p.VKN))
                    return true;
                return seen.Add(p.VKN);
            })
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Converts settlement batch totals to a canonical summary for ERP journal entries.
    /// </summary>
    public CanonicalSettlementSummary MapSettlementBatch(SettlementBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        return new CanonicalSettlementSummary
        {
            Platform = batch.Platform,
            PeriodStart = batch.PeriodStart,
            PeriodEnd = batch.PeriodEnd,
            TotalGross = batch.TotalGross,
            TotalCommission = batch.TotalCommission,
            TotalNet = batch.TotalNet,
            LineCount = batch.Lines.Count,
            FormattedGross = batch.TotalGross.ToString("F2", CultureInfo.InvariantCulture),
            FormattedCommission = batch.TotalCommission.ToString("F2", CultureInfo.InvariantCulture),
            FormattedNet = batch.TotalNet.ToString("F2", CultureInfo.InvariantCulture)
        };
    }
}

/// <summary>
/// Canonical settlement summary for ERP journal entry creation.
/// </summary>
public sealed class CanonicalSettlementSummary
{
    public string Platform { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalGross { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalNet { get; set; }
    public int LineCount { get; set; }
    public string FormattedGross { get; set; } = "0.00";
    public string FormattedCommission { get; set; } = "0.00";
    public string FormattedNet { get; set; } = "0.00";
}
