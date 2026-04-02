using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Invoice.Queries;

public sealed class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceListDto>>
{
    private readonly IInvoiceRepository _repository;
    private readonly ITenantProvider _tenantProvider;

    public GetInvoicesHandler(IInvoiceRepository repository, ITenantProvider tenantProvider)
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
    }

    public async Task<PagedResult<InvoiceListDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenantId = _tenantProvider.GetCurrentTenantId();
        var allInvoices = await _repository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var filtered = allInvoices.AsEnumerable();

        if (request.Type.HasValue)
            filtered = filtered.Where(i => i.Type == request.Type.Value);
        if (request.Status.HasValue)
            filtered = filtered.Where(i => i.Status == request.Status.Value);
        if (request.Platform.HasValue)
            filtered = filtered.Where(i => string.Equals(i.PlatformCode, request.Platform.Value.ToString(), StringComparison.Ordinal));
        if (request.From.HasValue)
            filtered = filtered.Where(i => i.InvoiceDate >= request.From.Value);
        if (request.To.HasValue)
            filtered = filtered.Where(i => i.InvoiceDate <= request.To.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            filtered = filtered.Where(i =>
                i.InvoiceNumber.Contains(request.Search, StringComparison.OrdinalIgnoreCase) ||
                i.CustomerName.Contains(request.Search, StringComparison.OrdinalIgnoreCase));

        var materialised = filtered.ToList();
        var total = materialised.Count;

        var items = materialised
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListDto(
                Id: i.Id,
                InvoiceNumber: i.InvoiceNumber,
                ExternalInvoiceId: i.GibInvoiceId,
                TypeName: i.Type.ToString(),
                TypeBadgeColor: i.Type switch
                {
                    Domain.Enums.InvoiceType.EFatura => "#0d6efd",
                    Domain.Enums.InvoiceType.EArsiv => "#198754",
                    Domain.Enums.InvoiceType.EIhracat => "#6f42c1",
                    _ => "#6c757d"
                },
                StatusName: i.Status.ToString(),
                StatusBadgeColor: i.Status switch
                {
                    Domain.Enums.InvoiceStatus.Sent => "#ffc107",
                    Domain.Enums.InvoiceStatus.Accepted => "#198754",
                    Domain.Enums.InvoiceStatus.Rejected => "#dc3545",
                    Domain.Enums.InvoiceStatus.Cancelled => "#6c757d",
                    _ => "#0d6efd"
                },
                RecipientName: i.CustomerName,
                RecipientVKN: i.CustomerTaxNumber,
                TotalAmount: i.GrandTotal,
                TaxRate: i.SubTotal > 0 ? (int)Math.Round(i.TaxTotal / i.SubTotal * 100) : 0,
                PlatformName: i.PlatformCode,
                ProviderName: i.Provider.ToString(),
                InvoiceDate: i.InvoiceDate,
                SentAt: i.SentAt,
                HasPdf: !string.IsNullOrEmpty(i.PdfUrl)))
            .ToList();

        return PagedResult<InvoiceListDto>.Create(items, total, request.Page, request.PageSize);
    }
}
