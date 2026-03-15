using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Komisyon kaydi — siparis bazinda platform komisyon detayi.
/// </summary>
public class CommissionRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string? OrderId { get; private set; }
    public string Platform { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal CommissionRate { get; private set; }
    public decimal CommissionAmount { get; private set; }
    public decimal ServiceFee { get; private set; }

    private CommissionRecord() { }

    public static CommissionRecord Create(
        Guid tenantId,
        string platform,
        decimal grossAmount,
        decimal commissionRate,
        decimal commissionAmount,
        decimal serviceFee,
        string? orderId = null,
        string? category = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platform);

        return new CommissionRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            Platform = platform,
            Category = category,
            GrossAmount = grossAmount,
            CommissionRate = commissionRate,
            CommissionAmount = commissionAmount,
            ServiceFee = serviceFee,
            CreatedAt = DateTime.UtcNow
        };
    }
}
