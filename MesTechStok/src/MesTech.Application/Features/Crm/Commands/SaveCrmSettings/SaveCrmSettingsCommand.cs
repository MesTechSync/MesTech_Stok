using MediatR;

namespace MesTech.Application.Features.Crm.Commands.SaveCrmSettings;

public record SaveCrmSettingsCommand(
    Guid TenantId,
    bool AutoAssignLeads,
    Guid? DefaultPipelineId,
    int LeadScoreThreshold,
    bool EnableEmailTracking
) : IRequest<SaveCrmSettingsResult>;
