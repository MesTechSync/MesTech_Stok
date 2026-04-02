using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Hesap kesimi satir detayi — siparis bazinda gelir/kesinti bilgisi.
/// </summary>
public sealed class SettlementLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid SettlementBatchId { get; private set; }
    public string? OrderId { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal CommissionAmount { get; private set; }
    public decimal ServiceFee { get; private set; }
    public decimal CargoDeduction { get; private set; }
    public decimal RefundDeduction { get; private set; }
    public decimal NetAmount { get; private set; }

    public byte[]? RowVersion { get; set; }

    // Navigation
    public SettlementBatch? SettlementBatch { get; private set; }

    private SettlementLine() { }

    public static SettlementLine Create(
        Guid tenantId,
        Guid settlementBatchId,
        string? orderId,
        decimal grossAmount,
        decimal commissionAmount,
        decimal serviceFee,
        decimal cargoDeduction,
        decimal refundDeduction,
        decimal netAmount)
    {
        return new SettlementLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SettlementBatchId = settlementBatchId,
            OrderId = orderId,
            GrossAmount = grossAmount,
            CommissionAmount = commissionAmount,
            ServiceFee = serviceFee,
            CargoDeduction = cargoDeduction,
            RefundDeduction = refundDeduction,
            NetAmount = netAmount,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Hesaplanan net tutar: brut - komisyon - servis - kargo - iade.
    /// </summary>
    public decimal CalculateNetAmount() =>
        GrossAmount - CommissionAmount - ServiceFee - CargoDeduction - RefundDeduction;
}
