using MesTech.Domain.Common;
using MesTech.Domain.Enums;

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
    public CommissionType CommissionType { get; private set; }
    public string? RateSource { get; private set; }

    private CommissionRecord() { }

    public static CommissionRecord Create(
        Guid tenantId,
        string platform,
        decimal grossAmount,
        decimal commissionRate,
        decimal commissionAmount,
        decimal serviceFee,
        string? orderId = null,
        string? category = null,
        CommissionType commissionType = CommissionType.Percentage,
        string? rateSource = null)
    {
        // Note: tenantId may be Guid.Empty when created by settlement parsers
        // (infrastructure layer). The caller/command handler sets the real tenant ID later.

        ArgumentException.ThrowIfNullOrWhiteSpace(platform);

        if (grossAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "Gross amount must be non-negative.");

        if (commissionRate < 0)
            throw new ArgumentOutOfRangeException(nameof(commissionRate), "Commission rate must be non-negative.");

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
            CommissionType = commissionType,
            RateSource = rateSource,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Net tutar: brut tutar - komisyon - servis ucreti.
    /// </summary>
    public decimal GetNetAmount() => GrossAmount - CommissionAmount - ServiceFee;
}
