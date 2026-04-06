namespace MesTech.Application.Queries.GetBrandById;

public sealed record GetBrandByIdResult(
    Guid Id,
    Guid TenantId,
    string Name,
    string? LogoUrl,
    bool IsActive,
    DateTime CreatedAt);
