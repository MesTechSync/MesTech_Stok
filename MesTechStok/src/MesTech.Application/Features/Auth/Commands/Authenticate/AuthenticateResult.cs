namespace MesTech.Application.Features.Auth.Commands.Authenticate;

public sealed class AuthenticateResult
{
    public bool IsSuccess { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Role { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthenticateResult Success(Guid userId, string userName, string? role, string token, string refreshToken)
        => new()
        {
            IsSuccess = true,
            UserId = userId,
            UserName = userName,
            Role = role,
            Token = token,
            RefreshToken = refreshToken
        };

    public static AuthenticateResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
