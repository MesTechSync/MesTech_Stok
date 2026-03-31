using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.RejectReturn;

/// <summary>
/// Iade talebini reddeder ve ret sebebini kaydeder.
/// </summary>
public sealed class RejectReturnHandler : IRequestHandler<RejectReturnCommand, RejectReturnResult>
{
    private readonly IReturnRequestRepository _returnRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RejectReturnHandler(
        IReturnRequestRepository returnRepo,
        IUnitOfWork unitOfWork)
    {
        _returnRepo = returnRepo ?? throw new ArgumentNullException(nameof(returnRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RejectReturnResult> Handle(
        RejectReturnCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var returnRequest = await _returnRepo.GetByIdAsync(request.ReturnRequestId).ConfigureAwait(false);
        if (returnRequest is null)
            return new RejectReturnResult
            {
                IsSuccess = false,
                ErrorMessage = $"ReturnRequest {request.ReturnRequestId} bulunamadi."
            };

        // Domain method — throws if status is not Pending
        returnRequest.Reject(request.RejectionReason);

        await _returnRepo.UpdateAsync(returnRequest).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RejectReturnResult
        {
            IsSuccess = true
        };
    }
}
