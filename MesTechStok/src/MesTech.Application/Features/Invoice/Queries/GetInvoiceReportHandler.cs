using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoiceReportHandler : IRequestHandler<GetInvoiceReportQuery, InvoiceReportDto>
{
    private readonly IInvoiceRepository _repository;

    public GetInvoiceReportHandler(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Task<InvoiceReportDto> Handle(GetInvoiceReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Wire real data: query invoices between From-To, filter by Platform, aggregate by type
        var report = new InvoiceReportDto(
            TotalCount: 0,
            TotalAmount: 0m,
            EFaturaCount: 0,
            EFaturaAmount: 0m,
            EArsivCount: 0,
            EArsivAmount: 0m,
            EIhracatCount: 0,
            EIhracatAmount: 0m,
            ByPlatform: new List<InvoiceReportByPlatformDto>());

        return Task.FromResult(report);
    }
}
