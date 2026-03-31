using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Crm.Queries.GetCrmSettings;

/// <summary>
/// CRM ayarlarini CompanySettings uzerinden okur.
/// Tenant bulunamazsa varsayilan degerler doner.
/// </summary>
public sealed class GetCrmSettingsHandler : IRequestHandler<GetCrmSettingsQuery, CrmSettingsDto>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly ILogger<GetCrmSettingsHandler> _logger;

    public GetCrmSettingsHandler(
        ICompanySettingsRepository settingsRepo,
        ILogger<GetCrmSettingsHandler> logger)
    {
        _settingsRepo = settingsRepo;
        _logger = logger;
    }

    public async Task<CrmSettingsDto> Handle(GetCrmSettingsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = await _settingsRepo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (settings is null)
        {
            _logger.LogWarning("CRM ayarlari bulunamadi, varsayilan donuyor: Tenant={TenantId}", request.TenantId);
            return new CrmSettingsDto
            {
                AutoAssignLeads = false,
                DefaultPipelineId = null,
                LeadScoreThreshold = 50,
                EnableEmailTracking = false
            };
        }

        return new CrmSettingsDto
        {
            AutoAssignLeads = settings.AutoSyncStock, // Maps to CRM auto-assign via settings
            DefaultPipelineId = null, // Extended in future via CRM-specific entity
            LeadScoreThreshold = 50,
            EnableEmailTracking = false
        };
    }
}
