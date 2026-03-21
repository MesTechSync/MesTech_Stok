using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.EInvoice;

namespace MesTech.Application.Features.EInvoice.Commands;

public class CreateEInvoiceHandler : IRequestHandler<CreateEInvoiceCommand, Guid>
{
    private readonly IEInvoiceDocumentRepository _repository;

    public CreateEInvoiceHandler(IEInvoiceDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateEInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gibUuid = Guid.NewGuid().ToString();
        var ettnNo = $"GGB{request.IssueDate.Year}{Guid.NewGuid():N}"[..20];

        var doc = EInvoiceDocument.Create(
            gibUuid: gibUuid,
            ettnNo: ettnNo,
            scenario: request.Scenario,
            type: request.Type,
            issueDate: request.IssueDate,
            sellerVkn: "0000000000",
            sellerTitle: "MesTech",
            buyerTitle: request.BuyerTitle,
            providerId: request.ProviderId,
            createdBy: "system");

        // Add lines
        int lineNo = 1;
        decimal totalLineExtension = 0m;
        decimal totalAllowance = 0m;
        decimal totalTax = 0m;

        foreach (var lineReq in request.Lines)
        {
            var line = EInvoiceLine.Create(
                documentId: doc.Id,
                lineNumber: lineNo++,
                description: lineReq.Description,
                quantity: lineReq.Quantity,
                unitCode: lineReq.UnitCode,
                unitPrice: lineReq.UnitPrice,
                taxPercent: lineReq.TaxPercent,
                allowanceAmount: lineReq.AllowanceAmount,
                productId: lineReq.ProductId);

            doc.AddLine(line);

            totalLineExtension += line.LineExtensionAmount;
            totalAllowance += line.AllowanceAmount;
            totalTax += line.TaxAmount;
        }

        var payable = totalLineExtension + totalTax;
        doc.SetFinancials(
            lineExtension: totalLineExtension,
            taxExclusive: totalLineExtension,
            taxInclusive: payable,
            allowance: totalAllowance,
            taxAmount: totalTax,
            payable: payable,
            currency: request.CurrencyCode);

        await _repository.AddAsync(doc, cancellationToken);

        return doc.Id;
    }
}
