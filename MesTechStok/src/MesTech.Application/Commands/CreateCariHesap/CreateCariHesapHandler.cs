using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateCariHesap;

public class CreateCariHesapHandler : IRequestHandler<CreateCariHesapCommand, Guid>
{
    private readonly ICariHesapRepository _cariHesapRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCariHesapHandler(ICariHesapRepository cariHesapRepository, IUnitOfWork unitOfWork)
    {
        _cariHesapRepository = cariHesapRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCariHesapCommand request, CancellationToken cancellationToken)
    {
        var cariHesap = new CariHesap
        {
            TenantId = request.TenantId,
            Name = request.Name,
            TaxNumber = request.TaxNumber,
            Type = request.Type,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
        };

        await _cariHesapRepository.AddAsync(cariHesap);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return cariHesap.Id;
    }
}
