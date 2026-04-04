using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.ProcessBotReturnRequest;

public record ProcessBotReturnRequestCommand : IRequest
{
    public string CustomerPhone { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string? ReturnReason { get; init; }
    public string RequestChannel { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class ProcessBotReturnRequestHandler : IRequestHandler<ProcessBotReturnRequestCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessBotReturnRequestHandler> _logger;

    public ProcessBotReturnRequestHandler(
        IOrderRepository orderRepository,
        IReturnRequestRepository returnRequestRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessBotReturnRequestHandler> logger)
    {
        _orderRepository = orderRepository;
        _returnRequestRepository = returnRequestRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProcessBotReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(request.OrderNumber, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            _logger.LogWarning("ProcessBotReturnRequest: Order not found — OrderNumber={OrderNumber}", request.OrderNumber);
            return;
        }

        var returnRequest = new ReturnRequest
        {
            TenantId = request.TenantId,
            OrderId = order.Id,
            Platform = default, // [Phase-2]: Add PlatformType.Bot or PlatformType.Manual enum value
            CustomerPhone = request.CustomerPhone,
            ReasonDetail = request.ReturnReason,
            RequestDate = DateTime.UtcNow,
            Notes = $"Bot return request — channel: {request.RequestChannel}"
        };

        await _returnRequestRepository.AddAsync(returnRequest, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ProcessBotReturnRequest: ReturnRequest created — OrderNumber={OrderNumber}, ReturnId={ReturnId}",
            request.OrderNumber, returnRequest.Id);
    }
}
