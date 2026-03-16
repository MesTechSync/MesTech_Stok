namespace MesTech.Application.Interfaces;

/// <summary>
/// Lightweight health-check contract for marketplace adapters.
/// PingAsync does NOT require credentials — it only verifies that
/// the platform's API endpoint is reachable (DNS + TCP + HTTP).
/// Adapters that lack a known public endpoint should NOT implement this.
/// </summary>
public interface IPingableAdapter
{
    /// <summary>
    /// Platform identifier (e.g. "Trendyol", "eBay").
    /// </summary>
    string PlatformCode { get; }

    /// <summary>
    /// Sends a lightweight HTTP request to the platform's base URL
    /// and returns true if any response is received within the timeout.
    /// Does not require authentication — a 401/403 still counts as "reachable".
    /// </summary>
    /// <param name="ct">Cancellation token (internal 5-second timeout also applies).</param>
    /// <returns>True if the endpoint responded; false on timeout/network error.</returns>
    Task<bool> PingAsync(CancellationToken ct = default);
}
