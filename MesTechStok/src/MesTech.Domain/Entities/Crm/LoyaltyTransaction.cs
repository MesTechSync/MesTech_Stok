using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Crm;

public class LoyaltyTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; private set; }
    public Guid LoyaltyProgramId { get; private set; }
    public int Points { get; private set; }
    public LoyaltyTransactionType Type { get; private set; }
    public string? Description { get; private set; }

    public LoyaltyProgram Program { get; private set; } = null!;

    private LoyaltyTransaction() { }

    public static LoyaltyTransaction Create(
        Guid tenantId, Guid customerId, Guid loyaltyProgramId,
        int points, LoyaltyTransactionType type, string? description = null)
    {
        if (points == 0) throw new ArgumentOutOfRangeException(nameof(points), "Points cannot be zero.");

        return new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = customerId,
            LoyaltyProgramId = loyaltyProgramId,
            Points = points,
            Type = type,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
