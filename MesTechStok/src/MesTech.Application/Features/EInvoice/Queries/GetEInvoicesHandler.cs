using MediatR;
using MesTech.Application.DTOs.EInvoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Application.Features.EInvoice.Queries;

public class GetEInvoicesHandler : IRequestHandler<GetEInvoicesQuery, PagedResult<EInvoiceDto>>
{
    private readonly IEInvoiceDocumentRepository _repository;

    public GetEInvoicesHandler(IEInvoiceDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<EInvoiceDto>> Handle(GetEInvoicesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            status: request.Status,
            from: request.From,
            to: request.To,
            page: request.Page,
            pageSize: request.PageSize,
            ct: cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return PagedResult<EInvoiceDto>.Create(dtos, total, request.Page, request.PageSize);
    }

    private static EInvoiceDto MapToDto(EInvoiceDocument doc) => new(
        Id: doc.Id,
        GibUuid: doc.GibUuid,
        EttnNo: doc.EttnNo,
        Scenario: doc.Scenario,
        Type: doc.Type,
        Status: doc.Status,
        IssueDate: doc.IssueDate,
        DueDate: doc.DueDate,
        SellerVkn: doc.SellerVkn,
        SellerTitle: doc.SellerTitle,
        BuyerVkn: doc.BuyerVkn,
        BuyerTitle: doc.BuyerTitle,
        PayableAmount: doc.PayableAmount,
        CurrencyCode: doc.CurrencyCode,
        ProviderId: doc.ProviderId,
        ProviderRef: doc.ProviderRef,
        PdfUrl: doc.PdfUrl,
        CreditUsed: doc.CreditUsed,
        CreatedAt: doc.CreatedAt);
}
