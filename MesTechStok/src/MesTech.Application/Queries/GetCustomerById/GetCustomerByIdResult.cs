namespace MesTech.Application.Queries.GetCustomerById;

/// <summary>
/// Customer GetById query result DTO.
/// </summary>
public sealed record GetCustomerByIdResult(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    string CustomerType,
    string? Email,
    string? Phone,
    string? City,
    string? Country,
    bool IsActive,
    bool IsBlocked,
    bool IsVip,
    decimal CurrentBalance,
    DateTime CreatedAt);
