using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.AcceptQuotation;

public class AcceptQuotationHandler : IRequestHandler<AcceptQuotationCommand, AcceptQuotationResult>
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptQuotationHandler(IQuotationRepository quotationRepository, IUnitOfWork unitOfWork)
    {
        _quotationRepository = quotationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AcceptQuotationResult> Handle(AcceptQuotationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var quotation = await _quotationRepository.GetByIdAsync(request.QuotationId);
        if (quotation is null)
            return new AcceptQuotationResult { IsSuccess = false, ErrorMessage = "Quotation not found." };

        try
        {
            quotation.Accept();
        }
        catch (InvalidOperationException ex)
        {
            return new AcceptQuotationResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        await _quotationRepository.UpdateAsync(quotation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AcceptQuotationResult { IsSuccess = true };
    }
}
