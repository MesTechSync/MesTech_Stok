using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.Application.Commands.SyncN11Products;

public record SyncN11ProductsCommand(Guid StoreId) : IRequest<SyncResultDto>;
