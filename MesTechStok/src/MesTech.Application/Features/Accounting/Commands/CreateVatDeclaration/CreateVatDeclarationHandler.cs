using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateVatDeclaration;

public sealed class CreateVatDeclarationHandler : IRequestHandler<CreateVatDeclarationCommand, Guid>
{
    private readonly IVatDeclarationRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateVatDeclarationHandler(IVatDeclarationRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateVatDeclarationCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotent — ayni donem zaten varsa return
        var existing = await _repository.GetByPeriodAsync(request.TenantId, request.Year, request.Month, ct)
            .ConfigureAwait(false);
        if (existing is not null)
            return existing.Id;

        var declaration = VatDeclaration.Create(request.TenantId, request.Year, request.Month);
        await _repository.AddAsync(declaration, ct).ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return declaration.Id;
    }
}
