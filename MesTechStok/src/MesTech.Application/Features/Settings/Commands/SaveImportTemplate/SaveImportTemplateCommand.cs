using MediatR;

namespace MesTech.Application.Features.Settings.Commands.SaveImportTemplate;

public record SaveImportTemplateCommand(
    Guid TenantId,
    string TemplateName,
    string FileFormat,
    Dictionary<string, string> ColumnMappings
) : IRequest<SaveImportTemplateResult>;
