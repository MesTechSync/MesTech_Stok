using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetBitrix24DealStatus;

public class GetBitrix24DealStatusHandler
    : IRequestHandler<GetBitrix24DealStatusQuery, Bitrix24DealStatusDto?>
{
    private readonly IBitrix24DealRepository _dealRepository;

    public GetBitrix24DealStatusHandler(IBitrix24DealRepository dealRepository)
    {
        _dealRepository = dealRepository ?? throw new ArgumentNullException(nameof(dealRepository));
    }

    public async Task<Bitrix24DealStatusDto?> Handle(
        GetBitrix24DealStatusQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var deal = await _dealRepository.GetByOrderIdAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (deal is null)
            return null;

        return new Bitrix24DealStatusDto
        {
            Bitrix24DealId = deal.Id,
            ExternalDealId = deal.ExternalDealId,
            OrderId = deal.OrderId,
            Title = deal.Title,
            Opportunity = deal.Opportunity,
            StageId = deal.StageId,
            Currency = deal.Currency,
            SyncStatus = deal.SyncStatus.ToString(),
            LastSyncDate = deal.LastSyncDate,
            SyncError = deal.SyncError
        };
    }
}
