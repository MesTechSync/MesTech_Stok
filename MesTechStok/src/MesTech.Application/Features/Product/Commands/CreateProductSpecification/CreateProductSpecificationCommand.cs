using MediatR;

namespace MesTech.Application.Features.Product.Commands.CreateProductSpecification;

public record CreateProductSpecificationCommand(
    Guid TenantId,
    Guid ProductId,
    string SpecGroup,
    string SpecName,
    string SpecValue,
    string? Unit = null,
    int DisplayOrder = 0
) : IRequest<Guid>;
