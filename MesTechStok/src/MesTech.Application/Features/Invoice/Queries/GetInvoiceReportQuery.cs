using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Invoice.Queries;

public record GetInvoiceReportQuery(
    DateTime From,
    DateTime To,
    PlatformType? Platform
) : IRequest<InvoiceReportDto>;
