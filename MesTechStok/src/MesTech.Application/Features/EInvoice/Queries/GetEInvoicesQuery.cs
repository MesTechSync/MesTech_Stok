using MesTech.Application.DTOs.EInvoice;
using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.EInvoice.Queries;

public record GetEInvoicesQuery(
    DateTime? From,
    DateTime? To,
    EInvoiceStatus? Status,
    string? ProviderId,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<EInvoiceDto>>;
