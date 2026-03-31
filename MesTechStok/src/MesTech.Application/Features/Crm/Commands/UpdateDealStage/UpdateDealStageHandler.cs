using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Crm.Commands.UpdateDealStage;

public sealed class UpdateDealStageHandler
    : IRequestHandler<UpdateDealStageCommand, UpdateDealStageResult>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateDealStageHandler> _logger;

    public UpdateDealStageHandler(IDealRepository dealRepository, IUnitOfWork uow, ILogger<UpdateDealStageHandler> logger)
    {
        _dealRepository = dealRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task<UpdateDealStageResult> Handle(
        UpdateDealStageCommand request, CancellationToken cancellationToken)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (deal is null)
            return new UpdateDealStageResult { ErrorMessage = $"Deal bulunamadi: {request.DealId}" };

        deal.MoveToStage(request.NewStageId);
        await _dealRepository.UpdateAsync(deal).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deal stage guncellendi: DealId={DealId}, NewStage={Stage}",
            request.DealId, request.NewStageId);

        return new UpdateDealStageResult { IsSuccess = true };
    }
}
