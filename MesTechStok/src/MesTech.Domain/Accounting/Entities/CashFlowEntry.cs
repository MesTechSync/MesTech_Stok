using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Nakit akisi girisi — gelis/gidis yonlu, kategorili.
/// </summary>
public class CashFlowEntry : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTime EntryDate { get; private set; }
    public decimal Amount { get; private set; }
    public CashFlowDirection Direction { get; private set; }
    public string? Category { get; private set; }
    public string? Description { get; private set; }
    public Guid? CounterpartyId { get; private set; }

    // Navigation
    public Counterparty? Counterparty { get; private set; }

    private CashFlowEntry() { }

    public static CashFlowEntry Create(
        Guid tenantId,
        DateTime entryDate,
        decimal amount,
        CashFlowDirection direction,
        string? category = null,
        string? description = null,
        Guid? counterpartyId = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        return new CashFlowEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntryDate = entryDate,
            Amount = amount,
            Direction = direction,
            Category = category,
            Description = description,
            CounterpartyId = counterpartyId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
