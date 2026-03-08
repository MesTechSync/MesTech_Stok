using MesTech.Application.DTOs;
using MediatR;

namespace MesTech.Application.Commands.SyncHepsiburadaProducts;

public record SyncHepsiburadaProductsCommand(Guid StoreId) : IRequest<SyncResultDto>;
