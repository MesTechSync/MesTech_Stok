using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Stopaj (tevkifat) kaydi — 9284 CB kuralina uygun.
/// Matrah = KDV haric tutar; komisyon/kargo matrahi degistirmez.
/// </summary>
public class TaxWithholding : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? InvoiceId { get; private set; }
    public decimal TaxExclusiveAmount { get; private set; }
    public decimal Rate { get; private set; }
    public decimal WithholdingAmount { get; private set; }
    public string TaxType { get; private set; } = string.Empty;

    private TaxWithholding() { }

    public static TaxWithholding Create(
        Guid tenantId,
        decimal taxExclusiveAmount,
        decimal rate,
        string taxType,
        Guid? invoiceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taxType);
        if (rate < 0 || rate > 1)
            throw new ArgumentOutOfRangeException(nameof(rate), "Rate must be between 0 and 1.");

        var withholdingAmount = taxExclusiveAmount * rate;

        var withholding = new TaxWithholding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            TaxExclusiveAmount = taxExclusiveAmount,
            Rate = rate,
            WithholdingAmount = withholdingAmount,
            TaxType = taxType,
            CreatedAt = DateTime.UtcNow
        };

        withholding.RaiseDomainEvent(new TaxWithholdingComputedEvent
        {
            TenantId = tenantId,
            TaxWithholdingId = withholding.Id,
            TaxExclusiveAmount = taxExclusiveAmount,
            Rate = rate,
            WithholdingAmount = withholdingAmount,
            TaxType = taxType
        });

        return withholding;
    }
}
