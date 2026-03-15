using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetCompanySettings;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// Desktop handler for GetCompanySettingsQuery.
/// H32: Migrated from Core.AppDbContext to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class GetCompanySettingsHandler : IRequestHandler<GetCompanySettingsQuery, CompanySettingsDto?>
{
    private readonly IServiceProvider _serviceProvider;

    public GetCompanySettingsHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
        catch
        {
            // Intentional: CompanySettings table may not exist on first run.
            return null;
        }
    }
}
