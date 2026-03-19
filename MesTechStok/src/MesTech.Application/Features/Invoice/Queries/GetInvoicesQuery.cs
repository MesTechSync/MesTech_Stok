using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Invoice.Queries;

public record GetInvoicesQuery(
    InvoiceType? Type,
    InvoiceStatus? Status,
    PlatformType? Platform,
    DateTime? From,
    DateTime? To,
    string? Search,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<InvoiceListDto>>;
