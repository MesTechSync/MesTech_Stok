namespace MesTech.Application.Features.Crm.Queries.GetCrmSettings;

public sealed class CrmSettingsDto
{
    public bool AutoAssignLeads { get; init; }
    public Guid? DefaultPipelineId { get; init; }
    public int LeadScoreThreshold { get; init; }
    public bool EnableEmailTracking { get; init; }
}
