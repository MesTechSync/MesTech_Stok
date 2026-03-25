using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Tasks;

public sealed class Project : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public string? Color { get; private set; }

    private Project() { }

    public static Project Create(
        Guid tenantId, string name, Guid? ownerUserId = null,
        string? description = null, DateTime? startDate = null,
        DateTime? dueDate = null, string? color = null, Guid? storeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Project
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = storeId,
            Name = name,
            Description = description,
            Status = ProjectStatus.Planning,
            StartDate = startDate,
            DueDate = dueDate,
            OwnerUserId = ownerUserId,
            Color = color,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        if (Status != ProjectStatus.Planning && Status != ProjectStatus.OnHold)
            throw new InvalidOperationException("Cannot start project from current status.");
        Status = ProjectStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ProjectStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PutOnHold()
    {
        Status = ProjectStatus.OnHold;
        UpdatedAt = DateTime.UtcNow;
    }
}
