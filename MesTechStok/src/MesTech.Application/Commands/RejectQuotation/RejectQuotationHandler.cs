using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.RejectQuotation;

public class RejectQuotationHandler : IRequestHandler<RejectQuotationCommand, RejectQuotationResult>
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectQuotationHandler(IQuotationRepository quotationRepository, IUnitOfWork unitOfWork)
    {
        _quotationRepository = quotationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RejectQuotationResult> Handle(RejectQuotationCommand request, CancellationToken cancellationToken)
    {
        var quotation = await _quotationRepository.GetByIdAsync(request.QuotationId);
        if (quotation is null)
            return new RejectQuotationResult { IsSuccess = false, ErrorMessage = "Quotation not found." };

        try
        {
            quotation.Reject();
        }
        catch (InvalidOperationException ex)
        {
            return new RejectQuotationResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        await _quotationRepository.UpdateAsync(quotation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RejectQuotationResult { IsSuccess = true };
    }
}
