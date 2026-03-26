namespace MesTech.Application.Interfaces;

/// <summary>
/// User authentication service — username/password login validation.
/// Infrastructure implements with BCrypt password verification.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> ValidateAsync(string username, string password, CancellationToken ct = default);
}

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }
    public Guid? UserId { get; init; }
    public Guid? TenantId { get; init; }
    public string? DisplayName { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Success(Guid userId, Guid tenantId, string displayName) =>
        new() { IsSuccess = true, UserId = userId, TenantId = tenantId, DisplayName = displayName };

    public static AuthResult Fail(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };
}
