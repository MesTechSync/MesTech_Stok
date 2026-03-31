using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CancelOrder;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, CancelOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CancelOrderResult> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            .ConfigureAwait(false);

        if (order is null)
            return new CancelOrderResult { IsSuccess = false, ErrorMessage = $"Order {request.OrderId} not found." };

        order.Cancel(request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CancelOrderResult { IsSuccess = true };
    }
}
