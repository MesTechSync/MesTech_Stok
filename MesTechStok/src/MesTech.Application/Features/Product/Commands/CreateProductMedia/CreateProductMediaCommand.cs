using MediatR;
using MesTech.Domain.Entities;

namespace MesTech.Application.Features.Product.Commands.CreateProductMedia;

public record CreateProductMediaCommand(
    Guid TenantId,
    Guid ProductId,
    MediaType Type,
    string Url,
    int SortOrder,
    Guid? VariantId = null,
    string? AltText = null
) : IRequest<Guid>;
