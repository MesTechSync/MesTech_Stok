using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoiceReportHandler : IRequestHandler<GetInvoiceReportQuery, InvoiceReportDto>
{
    private readonly IInvoiceRepository _repository;
    private readonly ITenantProvider _tenantProvider;

    public GetInvoiceReportHandler(IInvoiceRepository repository, ITenantProvider tenantProvider)
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
    }

    public async Task<InvoiceReportDto> Handle(GetInvoiceReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var allInvoices = await _repository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var filtered = allInvoices
            .Where(i => i.InvoiceDate >= request.From && i.InvoiceDate <= request.To);

        if (request.Platform.HasValue)
            filtered = filtered.Where(i => string.Equals(i.PlatformCode, request.Platform.Value.ToString(), StringComparison.Ordinal));

        var invoices = filtered.ToList();

        var byPlatform = invoices
            .Where(i => !string.IsNullOrEmpty(i.PlatformCode))
            .GroupBy(i => i.PlatformCode!)
            .Select(g => new InvoiceReportByPlatformDto(g.Key, g.Count(), g.Sum(i => i.GrandTotal)))
            .ToList();

        return new InvoiceReportDto(
            TotalCount: invoices.Count,
            TotalAmount: invoices.Sum(i => i.GrandTotal),
            EFaturaCount: invoices.Count(i => i.Type == InvoiceType.EFatura),
            EFaturaAmount: invoices.Where(i => i.Type == InvoiceType.EFatura).Sum(i => i.GrandTotal),
            EArsivCount: invoices.Count(i => i.Type == InvoiceType.EArsiv),
            EArsivAmount: invoices.Where(i => i.Type == InvoiceType.EArsiv).Sum(i => i.GrandTotal),
            EIhracatCount: invoices.Count(i => i.Type == InvoiceType.EIhracat),
            EIhracatAmount: invoices.Where(i => i.Type == InvoiceType.EIhracat).Sum(i => i.GrandTotal),
            ByPlatform: byPlatform);
    }
}
