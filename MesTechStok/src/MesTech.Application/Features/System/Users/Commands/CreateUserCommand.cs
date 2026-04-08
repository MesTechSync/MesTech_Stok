using MediatR;

namespace MesTech.Application.Features.System.Users.Commands;

/// <summary>
/// Yeni kullanıcı oluşturur. TenantId zorunlu — multi-tenant.
/// </summary>
public sealed record CreateUserCommand(
    Guid TenantId,
    string Username,
    string Password,
    string? Email,
    string? FirstName,
    string? LastName,
    string? Phone) : IRequest<CreateUserResult>;

public sealed record CreateUserResult(
    bool Success,
    Guid? UserId,
    string? ErrorMessage)
{
    public static CreateUserResult Ok(Guid userId) => new(true, userId, null);
    public static CreateUserResult Fail(string error) => new(false, null, error);
}
