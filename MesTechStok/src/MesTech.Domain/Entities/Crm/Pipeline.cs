using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Crm;

public class Pipeline : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public int Position { get; private set; }

    private readonly List<PipelineStage> _stages = [];
    public IReadOnlyCollection<PipelineStage> Stages => _stages.AsReadOnly();

    private Pipeline() { }

    public static Pipeline Create(Guid tenantId, string name, bool isDefault, int position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Pipeline
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            IsDefault = isDefault,
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
