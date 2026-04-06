using MediatR;

namespace MesTech.Application.Features.System.Users.Commands;

/// <summary>
/// Mevcut kullanıcı bilgilerini günceller. Şifre değiştirmez — ChangePassword ayrı.
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    bool? IsActive) : IRequest<UpdateUserResult>;

public sealed record UpdateUserResult(
    bool Success,
    string? ErrorMessage)
{
    public static UpdateUserResult Ok() => new(true, null);
    public static UpdateUserResult Fail(string error) => new(false, error);
}
