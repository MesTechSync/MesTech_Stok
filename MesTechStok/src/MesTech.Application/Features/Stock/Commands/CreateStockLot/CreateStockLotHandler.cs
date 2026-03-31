using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Stock.Commands.CreateStockLot;

public sealed class CreateStockLotHandler : IRequestHandler<CreateStockLotCommand, CreateStockLotResult>
{
    private readonly IStockLotRepository _lotRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateStockLotHandler> _logger;

    public CreateStockLotHandler(
        IStockLotRepository lotRepo,
        IUnitOfWork unitOfWork,
        ILogger<CreateStockLotHandler> logger)
    {
        _lotRepo = lotRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateStockLotResult> Handle(
        CreateStockLotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var lot = StockLot.Create(
                request.TenantId, request.ProductId, request.LotNumber,
                request.Quantity, request.UnitCost,
                request.WarehouseId, request.WarehouseName,
                request.SupplierId, request.SupplierName,
                request.ExpiryDate, request.Notes);

            await _lotRepo.AddAsync(lot, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("StockLot created: {LotId} — {LotNumber}, Qty={Qty}",
                lot.Id, request.LotNumber, request.Quantity);

            return CreateStockLotResult.Success(lot.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StockLot creation failed: {LotNumber}", request.LotNumber);
            return CreateStockLotResult.Failure(ex.Message);
        }
    }
}
