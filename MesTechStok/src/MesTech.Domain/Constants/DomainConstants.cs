namespace MesTech.Domain.Constants;

/// <summary>
/// Domain-level constants used across the application.
/// </summary>
public static class DomainConstants
{
    /// <summary>
    /// Well-known UserId for system-level operations (scheduled jobs, background tasks).
    /// Use instead of Guid.Empty when a UserId is required but no real user context exists.
    /// </summary>
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Well-known TenantId for cross-tenant system operations.
    /// </summary>
    public static readonly Guid SystemTenantId = new("00000000-0000-0000-0000-000000000002");
}
