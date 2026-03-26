using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateOrder;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Type = request.OrderType,
            Notes = request.Notes,
            OrderDate = DateTime.UtcNow,
            RequiredDate = request.RequiredDate,
        };

        await _orderRepository.AddAsync(order).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateOrderResult
        {
            IsSuccess = true,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
        };
    }
}
