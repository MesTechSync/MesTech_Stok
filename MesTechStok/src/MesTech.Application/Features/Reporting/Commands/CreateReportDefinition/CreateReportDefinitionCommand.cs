using MediatR;
using MesTech.Domain.Entities.Reporting;

namespace MesTech.Application.Features.Reporting.Commands.CreateReportDefinition;

public record CreateReportDefinitionCommand(
    Guid TenantId,
    string Name,
    ReportType Type,
    ReportFrequency Frequency,
    string? RecipientEmail = null
) : IRequest<Guid>;
