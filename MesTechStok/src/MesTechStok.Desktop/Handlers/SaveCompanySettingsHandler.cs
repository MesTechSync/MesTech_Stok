using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.SaveCompanySettings;
using MesTech.Domain.Entities;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated from Core.AppDbContext to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class SaveCompanySettingsHandler : IRequestHandler<SaveCompanySettingsCommand, SaveCompanySettingsResult>
{
    private readonly IServiceProvider _serviceProvider;

    public SaveCompanySettingsHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<SaveCompanySettingsResult> Handle(SaveCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

            var settings = await db.CompanySettings.FirstOrDefaultAsync(cancellationToken);
            if (settings == null)
            {
                settings = new CompanySettings();
                db.CompanySettings.Add(settings);
            }

            settings.CompanyName = request.CompanyName;
            settings.TaxNumber = request.TaxNumber;
            settings.Phone = request.Phone;
            settings.Email = request.Email;
            settings.Address = request.Address;
            settings.UpdatedAt = DateTime.Now;

            var existing = await db.Warehouses.ToListAsync(cancellationToken);
            if (existing.Count > 0)
            {
                db.Warehouses.RemoveRange(existing);
            }

            foreach (var w in request.Warehouses)
            {
                db.Warehouses.Add(new Warehouse
                {
                    Name = w.Name.Trim(),
                    Address = string.IsNullOrWhiteSpace(w.Address) ? null : w.Address.Trim(),
                    City = string.IsNullOrWhiteSpace(w.City) ? null : w.City.Trim(),
                    Phone = string.IsNullOrWhiteSpace(w.Phone) ? null : w.Phone.Trim(),
                    Type = "BRANCH",
                    IsActive = true,
                });
            }

            await db.SaveChangesAsync(cancellationToken);

            return new SaveCompanySettingsResult { IsSuccess = true };
        }
        catch (Exception ex)
        {
            return new SaveCompanySettingsResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
            };
        }
    }
}
