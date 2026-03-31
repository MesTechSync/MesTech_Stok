using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, UpdateOrderStatusResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderStatusHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdateOrderStatusResult> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
            return new UpdateOrderStatusResult { IsSuccess = false, ErrorMessage = $"Order {request.OrderId} not found." };

        switch (request.NewStatus)
        {
            case OrderStatus.Confirmed:
                order.Place();
                break;

            case OrderStatus.Shipped:
                if (string.IsNullOrWhiteSpace(request.TrackingNumber))
                    return new UpdateOrderStatusResult { IsSuccess = false, ErrorMessage = "TrackingNumber is required for shipping." };
                order.MarkAsShipped(request.TrackingNumber, request.CargoProvider ?? CargoProvider.None);
                break;

            case OrderStatus.Delivered:
                order.MarkAsDelivered();
                break;

            case OrderStatus.Cancelled:
                order.Cancel();
                break;

            default:
                return new UpdateOrderStatusResult { IsSuccess = false, ErrorMessage = $"Unsupported status transition to {request.NewStatus}." };
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdateOrderStatusResult { IsSuccess = true };
    }
}
