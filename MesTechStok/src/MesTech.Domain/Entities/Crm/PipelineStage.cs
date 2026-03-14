using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Crm;

public class PipelineStage : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Position { get; private set; }
    public decimal? Probability { get; private set; }
    public StageType Type { get; private set; }
    public string? Color { get; private set; }

    public Pipeline Pipeline { get; private set; } = null!;

    private PipelineStage() { }

    public static PipelineStage Create(
        Guid tenantId, Guid pipelineId, string name,
        int position, decimal? probability, StageType type, string? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (probability is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be 0-100.");

        return new PipelineStage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PipelineId = pipelineId,
            Name = name,
            Position = position,
            Probability = probability,
            Type = type,
            Color = color,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdatePosition(int newPosition)
    {
        Position = newPosition;
        UpdatedAt = DateTime.UtcNow;
    }
}
