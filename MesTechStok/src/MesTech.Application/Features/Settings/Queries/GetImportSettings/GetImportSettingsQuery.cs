using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetImportSettings;

public record GetImportSettingsQuery(Guid TenantId) : IRequest<ImportSettingsDto>;

public sealed class ImportSettingsDto
{
    public List<ImportTemplateDto> Templates { get; set; } = new();
    public int TotalCount { get; set; }
}

public sealed class ImportTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public string Format { get; set; } = string.Empty;
    public DateTime? LastUsedAt { get; set; }
    public List<ImportFieldMappingDto> Mappings { get; set; } = new();
}

public sealed class ImportFieldMappingDto
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}
