namespace MesTech.Application.DTOs.Crm;
/// <summary>
/// Kanban Board data transfer object.
/// </summary>
public sealed class KanbanBoardDto
{
    public Guid PipelineId { get; set; }
    public string PipelineName { get; set; } = string.Empty;
    public IReadOnlyList<KanbanStageDto> Stages { get; set; } = [];
}

public sealed class KanbanStageDto
{
    public Guid StageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int Position { get; set; }
    public decimal Probability { get; set; }
    public IReadOnlyList<DealDto> Deals { get; set; } = [];
    public decimal TotalAmount => Deals.Sum(d => d.Amount);
}
