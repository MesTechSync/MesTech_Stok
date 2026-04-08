using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.Application.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto?>;
