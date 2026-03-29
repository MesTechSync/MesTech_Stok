using MediatR;

namespace MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;

public record DeleteErpAccountMappingCommand(Guid TenantId, Guid MappingId) : IRequest<bool>;
