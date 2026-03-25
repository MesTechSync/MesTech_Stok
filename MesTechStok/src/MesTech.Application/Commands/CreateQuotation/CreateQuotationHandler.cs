using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateQuotation;

public sealed class CreateQuotationHandler : IRequestHandler<CreateQuotationCommand, CreateQuotationResult>
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuotationHandler(IQuotationRepository quotationRepository, IUnitOfWork unitOfWork)
    {
        _quotationRepository = quotationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateQuotationResult> Handle(CreateQuotationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var quotation = new Quotation
        {
            QuotationNumber = request.QuotationNumber,
            ValidUntil = request.ValidUntil,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerTaxNumber = request.CustomerTaxNumber,
            CustomerTaxOffice = request.CustomerTaxOffice,
            CustomerAddress = request.CustomerAddress,
            CustomerEmail = request.CustomerEmail,
            Notes = request.Notes,
            Terms = request.Terms,
        };

        if (request.Lines is { Count: > 0 })
        {
            foreach (var lineInput in request.Lines)
            {
                var line = new QuotationLine
                {
                    QuotationId = quotation.Id,
                    ProductId = lineInput.ProductId,
                    ProductName = lineInput.ProductName,
                    SKU = lineInput.SKU,
                    Quantity = lineInput.Quantity,
                    UnitPrice = lineInput.UnitPrice,
                    TaxRate = lineInput.TaxRate,
                    Description = lineInput.Description,
                };

                quotation.AddLine(line);
            }
        }

        await _quotationRepository.AddAsync(quotation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateQuotationResult
        {
            IsSuccess = true,
            QuotationId = quotation.Id
        };
    }
}
