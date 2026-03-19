using MesTech.Application.Features.Invoice.DTOs;
using MediatR;

namespace MesTech.Application.Features.Invoice.Queries;

public record GetInvoiceProvidersQuery() : IRequest<List<InvoiceProviderStatusDto>>;
