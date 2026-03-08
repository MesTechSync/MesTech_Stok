using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.Application.Commands.SyncCiceksepetiProducts;

public record SyncCiceksepetiProductsCommand(Guid StoreId) : IRequest<SyncResultDto>;
