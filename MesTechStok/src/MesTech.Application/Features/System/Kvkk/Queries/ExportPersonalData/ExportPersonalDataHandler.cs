using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;

public class ExportPersonalDataHandler : IRequestHandler<ExportPersonalDataQuery, PersonalDataExportDto>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly ILogger<ExportPersonalDataHandler> _logger;

    public ExportPersonalDataHandler(ITenantRepository tenantRepo, ILogger<ExportPersonalDataHandler> logger)
    {
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    public async Task<PersonalDataExportDto> Handle(ExportPersonalDataQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("KVKK veri disari aktarma talebi: TenantId={TenantId}", request.TenantId);

        var tenant = await _tenantRepo.GetByIdAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant bulunamadi: {request.TenantId}");

        // Gercek uygulamada: User, Store, Order, Product, CariHesap vb. toplanir
        var export = new PersonalDataExportDto
        {
            TenantId = request.TenantId,
            ExportedAt = DateTime.UtcNow,
            TenantName = tenant.Name,
            UserCount = 0,
            StoreCount = 0,
            OrderCount = 0,
            ProductCount = 0,
            DataJson = global::System.Text.Json.JsonSerializer.Serialize(new
            {
                tenant = new { tenant.Id, tenant.Name, tenant.CreatedAt },
                exportedAt = DateTime.UtcNow,
                note = "KVKK madde 11/c uyarinca kisisel veri aktarimi"
            })
        };

        return export;
    }
}
