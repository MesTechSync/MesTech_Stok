using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Invoice.Commands;

public record BulkCreateInvoiceCommand(
    List<Guid> OrderIds,
    InvoiceProvider Provider
) : IRequest<BulkInvoiceResultDto>;
