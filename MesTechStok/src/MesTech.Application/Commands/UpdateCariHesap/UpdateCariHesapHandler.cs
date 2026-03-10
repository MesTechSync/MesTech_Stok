using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateCariHesap;

public class UpdateCariHesapHandler : IRequestHandler<UpdateCariHesapCommand, bool>
{
    private readonly ICariHesapRepository _cariHesapRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCariHesapHandler(ICariHesapRepository cariHesapRepository, IUnitOfWork unitOfWork)
    {
        _cariHesapRepository = cariHesapRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateCariHesapCommand request, CancellationToken cancellationToken)
    {
        var cariHesap = await _cariHesapRepository.GetByIdAsync(request.Id);
        if (cariHesap is null)
            return false;

        cariHesap.Name = request.Name;
        cariHesap.TaxNumber = request.TaxNumber;
        cariHesap.Type = request.Type;
        cariHesap.Phone = request.Phone;
        cariHesap.Email = request.Email;
        cariHesap.Address = request.Address;

        await _cariHesapRepository.UpdateAsync(cariHesap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
