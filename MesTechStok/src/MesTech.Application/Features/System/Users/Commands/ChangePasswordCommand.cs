using MediatR;

namespace MesTech.Application.Features.System.Users.Commands;

/// <summary>
/// Kullanıcı şifresini değiştirir. Mevcut şifre doğrulaması yapılır.
/// </summary>
public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<ChangePasswordResult>;

public sealed record ChangePasswordResult(
    bool Success,
    string? ErrorMessage)
{
    public static ChangePasswordResult Ok() => new(true, null);
    public static ChangePasswordResult Fail(string error) => new(false, error);
}
