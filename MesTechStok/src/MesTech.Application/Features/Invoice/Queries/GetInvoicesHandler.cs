using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceListDto>>
{
    private readonly IInvoiceRepository _repository;

    public GetInvoicesHandler(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<InvoiceListDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Wire repository GetPaged with filters (Type, Status, Platform, From, To, Search)
        // var (items, total) = await _repository.GetPagedAsync(...);
        // var dtos = items.Select(MapToDto).ToList();
        // return PagedResult<InvoiceListDto>.Create(dtos, total, request.Page, request.PageSize);

        var result = PagedResult<InvoiceListDto>.Empty(request.Page, request.PageSize);
        return Task.FromResult(result);
    }
}
