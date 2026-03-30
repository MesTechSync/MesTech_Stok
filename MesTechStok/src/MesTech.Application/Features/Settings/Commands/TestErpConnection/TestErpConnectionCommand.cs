using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Settings.Commands.TestErpConnection;

public record TestErpConnectionCommand(
    Guid TenantId,
    ErpProvider ErpProvider
) : IRequest<TestErpConnectionResult>;
