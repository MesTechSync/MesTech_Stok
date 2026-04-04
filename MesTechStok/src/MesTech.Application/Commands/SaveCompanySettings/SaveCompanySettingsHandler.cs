using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.SaveCompanySettings;

public sealed class SaveCompanySettingsHandler : IRequestHandler<SaveCompanySettingsCommand, SaveCompanySettingsResult>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public SaveCompanySettingsHandler(
        ICompanySettingsRepository settingsRepo,
        IWarehouseRepository warehouseRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _settingsRepo = settingsRepo;
        _warehouseRepo = warehouseRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<SaveCompanySettingsResult> Handle(
        SaveCompanySettingsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var existing = await _settingsRepo.GetByTenantIdAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var settings = new CompanySettings
            {
                TenantId = tenantId,
                CompanyName = request.CompanyName,
                TaxNumber = request.TaxNumber,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };
            await _settingsRepo.AddAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.CompanyName = request.CompanyName;
            existing.TaxNumber = request.TaxNumber;
            existing.Phone = request.Phone;
            existing.Email = request.Email;
            existing.Address = request.Address;
            existing.UpdatedAt = DateTime.UtcNow;
            await _settingsRepo.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        // Depo kayıtları
        if (request.Warehouses is { Count: > 0 })
        {
            foreach (var wh in request.Warehouses)
            {
                var warehouse = new Warehouse
                {
                    TenantId = tenantId,
                    Name = wh.Name,
                    Address = wh.Address,
                    City = wh.City,
                    Phone = wh.Phone,
                    Code = $"WH-{DateTime.UtcNow.Ticks % 100000}"
                };
                await _warehouseRepo.AddAsync(warehouse, cancellationToken).ConfigureAwait(false);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SaveCompanySettingsResult { IsSuccess = true };
    }
}
