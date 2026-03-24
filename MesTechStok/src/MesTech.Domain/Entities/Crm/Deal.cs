using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Crm;

namespace MesTech.Domain.Entities.Crm;

public class Deal : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Guid? CrmContactId { get; private set; }
    public Guid PipelineId { get; private set; }
    public Guid StageId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateTime? ExpectedCloseDate { get; private set; }
    public DealStatus Status { get; private set; }
    public string? LostReason { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid? OrderId { get; private set; }

    public Pipeline Pipeline { get; private set; } = null!;
    public PipelineStage Stage { get; private set; } = null!;
    public CrmContact? Contact { get; private set; }

    private Deal() { }

    public static Deal Create(
        Guid tenantId, string title, Guid pipelineId, Guid stageId,
        decimal amount, Guid? crmContactId = null,
        DateTime? expectedCloseDate = null, Guid? assignedToUserId = null,
        Guid? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        return new Deal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = storeId,
            Title = title,
            PipelineId = pipelineId,
            StageId = stageId,
            Amount = amount,
            CrmContactId = crmContactId,
            ExpectedCloseDate = expectedCloseDate,
            AssignedToUserId = assignedToUserId,
            Status = DealStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MoveToStage(Guid newStageId)
    {
        if (Status != DealStatus.Open)
            throw new InvalidOperationException("Cannot move a closed deal to another stage.");

        var fromStageId = StageId;
        StageId = newStageId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DealStageChangedEvent(Id, TenantId, fromStageId, newStageId, DateTime.UtcNow));
    }

    public void MarkAsWon(Guid? orderId = null)
    {
        if (Status != DealStatus.Open)
            throw new InvalidOperationException("Deal is not open.");

        Status = DealStatus.Won;
        OrderId = orderId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DealWonEvent(Id, TenantId, orderId, Amount, DateTime.UtcNow));
    }

    public void MarkAsLost(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != DealStatus.Open)
            throw new InvalidOperationException("Deal is not open.");

        Status = DealStatus.Lost;
        LostReason = reason;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DealLostEvent(Id, TenantId, reason, DateTime.UtcNow));
    }

    public void LinkOrder(Guid orderId)
    {
        OrderId = orderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAmount(decimal newAmount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newAmount);
        Amount = newAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}
