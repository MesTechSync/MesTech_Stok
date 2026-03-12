using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.PushOrderToBitrix24;

public class PushOrderToBitrix24Handler
    : IRequestHandler<PushOrderToBitrix24Command, PushOrderToBitrix24Result>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IBitrix24DealRepository _dealRepository;
    private readonly IBitrix24Adapter _adapter;
    private readonly IUnitOfWork _unitOfWork;

    public PushOrderToBitrix24Handler(
        IOrderRepository orderRepository,
        IBitrix24DealRepository dealRepository,
        IBitrix24Adapter adapter,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _dealRepository = dealRepository ?? throw new ArgumentNullException(nameof(dealRepository));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PushOrderToBitrix24Result> Handle(
        PushOrderToBitrix24Command request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check if deal already exists for this order
        var existingDeal = await _dealRepository.GetByOrderIdAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (existingDeal is not null)
        {
            return new PushOrderToBitrix24Result
            {
                IsSuccess = true,
                ExternalDealId = existingDeal.ExternalDealId,
                Bitrix24DealId = existingDeal.Id
            };
        }

        // Get the order
        var order = await _orderRepository.GetByIdAsync(request.OrderId).ConfigureAwait(false);
        if (order is null)
        {
            return new PushOrderToBitrix24Result
            {
                IsSuccess = false,
                ErrorMessage = $"Order {request.OrderId} not found"
            };
        }

        try
        {
            // Push to Bitrix24
            var externalDealId = await _adapter.PushDealAsync(order, cancellationToken)
                .ConfigureAwait(false);

            if (externalDealId is null)
            {
                return new PushOrderToBitrix24Result
                {
                    IsSuccess = false,
                    ErrorMessage = "Bitrix24 adapter returned null deal ID"
                };
            }

            // Create local mapping
            var deal = new Bitrix24Deal
            {
                OrderId = order.Id,
                ExternalDealId = externalDealId.Value.ToString(),
                Title = $"Order #{order.OrderNumber}",
                Opportunity = order.TotalAmount,
                SyncStatus = SyncStatus.Synced,
                LastSyncDate = DateTime.UtcNow
            };

            await _dealRepository.AddAsync(deal, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new PushOrderToBitrix24Result
            {
                IsSuccess = true,
                ExternalDealId = deal.ExternalDealId,
                Bitrix24DealId = deal.Id
            };
        }
        catch (Exception ex)
        {
            return new PushOrderToBitrix24Result
            {
                IsSuccess = false,
                ErrorMessage = $"Bitrix24 push failed: {ex.Message}"
            };
        }
    }
}
