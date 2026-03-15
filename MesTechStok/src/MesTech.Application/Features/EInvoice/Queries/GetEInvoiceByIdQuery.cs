using MesTech.Application.DTOs.EInvoice;
using MediatR;

namespace MesTech.Application.Features.EInvoice.Queries;

public record GetEInvoiceByIdQuery(Guid EInvoiceId) : IRequest<EInvoiceDto?>;
