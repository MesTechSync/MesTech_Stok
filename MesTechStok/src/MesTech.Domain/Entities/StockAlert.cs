using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Stok alarm kaydı — düşük/kritik/sıfır stok uyarıları.
/// </summary>
public class StockAlert : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public StockAlertLevel AlertLevel { get; set; }
    public int CurrentStock { get; set; }
    public int ThresholdStock { get; set; }
    public string? Message { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime AlertDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public Product Product { get; set; } = null!;

    public void Resolve(string resolvedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
