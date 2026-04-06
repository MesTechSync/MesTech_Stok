using Mapster;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Queries.GetInvoiceById;

public sealed class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    private readonly IInvoiceRepository _repository;

    public GetInvoiceByIdHandler(IInvoiceRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return entity?.Adapt<InvoiceDto>();
    }
}
