using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTech.Application.Queries.GetCompanySettings;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// Desktop handler for GetCompanySettingsQuery.
/// H32: Migrated to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class GetCompanySettingsHandler : IRequestHandler<GetCompanySettingsQuery, CompanySettingsDto?>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GetCompanySettingsHandler>? _logger;

    public GetCompanySettingsHandler(IServiceProvider serviceProvider, ILogger<GetCompanySettingsHandler>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<CompanySettingsDto?> Handle(GetCompanySettingsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

            var settings = await db.CompanySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null) return null;

            return new CompanySettingsDto
            {
                CompanyName = settings.CompanyName,
                TaxNumber = settings.TaxNumber,
                Phone = settings.Phone,
                Email = settings.Email,
                Address = settings.Address,
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "{ClassName} - {Context}", nameof(GetCompanySettingsHandler), "CompanySettings table may not exist on first run");
            return null;
        }
    }
}
