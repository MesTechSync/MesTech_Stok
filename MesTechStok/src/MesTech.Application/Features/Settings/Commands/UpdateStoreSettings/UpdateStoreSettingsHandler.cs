using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;

public sealed class UpdateStoreSettingsHandler : IRequestHandler<UpdateStoreSettingsCommand, bool>
{
    private readonly ICompanySettingsRepository _settingsRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStoreSettingsHandler(ICompanySettingsRepository settingsRepo, IUnitOfWork unitOfWork)
    {
        _settingsRepo = settingsRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateStoreSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepo.GetByTenantIdAsync(request.TenantId, cancellationToken);

        if (settings is null)
        {
            settings = new CompanySettings
            {
                TenantId = request.TenantId,
                CompanyName = request.CompanyName,
                TaxNumber = request.TaxNumber,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address
            };
            await _settingsRepo.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.CompanyName = request.CompanyName;
            settings.TaxNumber = request.TaxNumber;
            settings.Phone = request.Phone;
            settings.Email = request.Email;
            settings.Address = request.Address;
            await _settingsRepo.UpdateAsync(settings, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
