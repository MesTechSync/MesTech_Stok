using MediatR;
using MesTech.Application.DTOs.EInvoice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Application.Features.EInvoice.Queries;

public class GetEInvoiceByIdHandler : IRequestHandler<GetEInvoiceByIdQuery, EInvoiceDto?>
{
    private readonly IEInvoiceDocumentRepository _repository;

    public GetEInvoiceByIdHandler(IEInvoiceDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<EInvoiceDto?> Handle(GetEInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var doc = await _repository.GetByIdAsync(request.EInvoiceId, cancellationToken);
        return doc is null ? null : MapToDto(doc);
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
