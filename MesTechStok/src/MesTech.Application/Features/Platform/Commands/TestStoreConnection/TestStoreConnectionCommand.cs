using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Features.Platform.Commands.TestStoreConnection;

public record TestStoreConnectionCommand(Guid StoreId) : IRequest<ConnectionTestResultDto>;
