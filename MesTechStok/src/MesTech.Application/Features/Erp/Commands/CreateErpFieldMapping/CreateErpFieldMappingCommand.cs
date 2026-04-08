using MediatR;

namespace MesTech.Application.Features.Erp.Commands.CreateErpFieldMapping;

public record CreateErpFieldMappingCommand(
    Guid TenantId,
    string ErpType,
    string MesTechField,
    string ErpField,
    bool IsRequired = false,
    string? TransformExpression = null
) : IRequest<Guid>;
