using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetImportSettings;

public sealed class GetImportSettingsHandler : IRequestHandler<GetImportSettingsQuery, ImportSettingsDto>
{
    private readonly IImportTemplateRepository _templateRepo;

    public GetImportSettingsHandler(IImportTemplateRepository templateRepo)
        => _templateRepo = templateRepo ?? throw new ArgumentNullException(nameof(templateRepo));

    public async Task<ImportSettingsDto> Handle(GetImportSettingsQuery request, CancellationToken cancellationToken)
    {
        var templates = await _templateRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return new ImportSettingsDto
        {
            TotalCount = templates.Count,
            Templates = templates.Select(t => new ImportTemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                FieldCount = t.FieldCount,
                Format = t.Format,
                LastUsedAt = t.LastUsedAt,
                Mappings = t.Mappings.Select(m => new ImportFieldMappingDto
                {
                    SourceColumn = m.SourceColumn,
                    TargetField = m.TargetField
                }).ToList()
            }).ToList()
        };
    }
}
