using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.Application.Commands.SyncTrendyolProducts;

public record SyncTrendyolProductsCommand(Guid StoreId) : IRequest<SyncResultDto>;
