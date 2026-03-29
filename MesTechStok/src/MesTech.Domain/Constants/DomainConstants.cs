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
    /// System user display name for audit trails and CreatedBy/UpdatedBy fields.
    /// Replaces hardcoded "system" strings across handlers.
    /// </summary>
    public const string SystemUserName = "system";

    /// <summary>
    /// Well-known TenantId for cross-tenant system operations.
    /// </summary>
    public static readonly Guid SystemTenantId = new("00000000-0000-0000-0000-000000000002");

    /// <summary>
    /// Maximum length for error message fields stored in the database.
    /// Used by FeedImportLog, SocialFeedConfiguration, etc.
    /// </summary>
    public const int MaxErrorMessageLength = 2000;

    /// <summary>
    /// Maximum length for sync error fields (shorter for high-volume sync logs).
    /// Used by Invoice Parasut sync, adapter sync errors.
    /// </summary>
    public const int MaxSyncErrorLength = 500;

    /// <summary>
    /// Truncates a string to the specified max length, returning null for null input.
    /// </summary>
    public static string? Truncate(string? value, int maxLength)
        => value is not null && value.Length > maxLength ? value[..maxLength] : value;
}
