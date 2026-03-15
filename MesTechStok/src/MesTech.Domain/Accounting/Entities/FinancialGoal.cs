using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Entities;

/// <summary>
/// Finansal hedef — hedef tutar, mevcut tutar ve basari durumu takibi.
/// </summary>
public class FinancialGoal : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; private set; } = string.Empty;
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsAchieved { get; private set; }

    private FinancialGoal() { }

    public static FinancialGoal Create(
        Guid tenantId,
        string title,
        decimal targetAmount,
        DateTime startDate,
        DateTime endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (targetAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetAmount), "Target amount must be positive.");
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));

        return new FinancialGoal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            TargetAmount = targetAmount,
            CurrentAmount = 0,
            StartDate = startDate,
            EndDate = endDate,
            IsAchieved = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProgress(decimal currentAmount)
    {
        CurrentAmount = currentAmount;
        IsAchieved = currentAmount >= TargetAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}
