using MediatR;

namespace MesTech.Application.Features.Settings.Commands.TestApiConnection;

public record TestApiConnectionCommand(
    Guid TenantId,
    string ApiBaseUrl
) : IRequest<TestApiConnectionResult>;
