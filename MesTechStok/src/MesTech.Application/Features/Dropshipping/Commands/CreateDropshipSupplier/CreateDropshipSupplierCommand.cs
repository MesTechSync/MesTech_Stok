using MediatR;
using MesTech.Domain.Dropshipping.Enums;

namespace MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;

public record CreateDropshipSupplierCommand(
    Guid TenantId,
    string Name,
    string? WebsiteUrl,
    DropshipMarkupType MarkupType,
    decimal MarkupValue
) : IRequest<Guid>;
