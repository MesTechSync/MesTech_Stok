using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Crm;

public class LoyaltyProgram : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public decimal PointsPerPurchase { get; private set; }
    public int MinRedeemPoints { get; private set; }
    public bool IsActive { get; private set; } = true;

    private LoyaltyProgram() { }

    public static LoyaltyProgram Create(
        Guid tenantId, string name, decimal pointsPerPurchase, int minRedeemPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pointsPerPurchase);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minRedeemPoints);

        return new LoyaltyProgram
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            PointsPerPurchase = pointsPerPurchase,
            MinRedeemPoints = minRedeemPoints,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateRules(decimal pointsPerPurchase, int minRedeemPoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pointsPerPurchase);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minRedeemPoints);

        PointsPerPurchase = pointsPerPurchase;
        MinRedeemPoints = minRedeemPoints;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
