using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateCariHareket;

public sealed class CreateCariHareketHandler : IRequestHandler<CreateCariHareketCommand, Guid>
{
    private readonly ICariHareketRepository _cariHareketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCariHareketHandler(ICariHareketRepository cariHareketRepository, IUnitOfWork unitOfWork)
    {
        _cariHareketRepository = cariHareketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCariHareketCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hareket = new CariHareket
        {
            TenantId = request.TenantId,
            CariHesapId = request.CariHesapId,
            Amount = request.Amount,
            Direction = request.Direction,
            Description = request.Description,
            Date = request.Date ?? DateTime.UtcNow,
            InvoiceId = request.InvoiceId,
            OrderId = request.OrderId,
        };

        await _cariHareketRepository.AddAsync(hareket, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return hareket.Id;
    }
}
