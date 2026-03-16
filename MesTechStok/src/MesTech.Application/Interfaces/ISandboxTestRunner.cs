namespace MesTech.Application.Interfaces;

/// <summary>
/// Sandbox test framework — tests adapter connectivity against sandbox/test environments.
/// Each adapter is exercised through: connection, authentication, and lightweight data retrieval.
/// Demir Kural 4: Sandbox ZORUNLU — production'a test verisi GITMEZ.
/// </summary>
public interface ISandboxTestRunner
{
    /// <summary>
    /// Tests a single adapter identified by platform code (e.g. "Trendyol", "Hepsiburada").
    /// </summary>
    Task<SandboxTestResult> TestAdapterAsync(string platform, CancellationToken ct);

    /// <summary>
    /// Tests all registered adapters and returns results for each.
    /// </summary>
    Task<List<SandboxTestResult>> TestAllAsync(CancellationToken ct);
}

/// <summary>
/// Structured result from a sandbox adapter test run.
/// </summary>
/// <param name="Platform">Platform code (e.g. "Trendyol", "OpenCart").</param>
/// <param name="ConnectionOk">True if the platform endpoint was reachable (DNS + TCP + HTTP).</param>
/// <param name="AuthOk">True if TestConnectionAsync returned success.</param>
/// <param name="DataOk">True if a lightweight data call (e.g. PullProducts/GetCategories) succeeded.</param>
/// <param name="ResponseTime">Total elapsed time for all checks.</param>
/// <param name="Error">Error message if any check failed; null on full success.</param>
public record SandboxTestResult(
    string Platform,
    bool ConnectionOk,
    bool AuthOk,
    bool DataOk,
    TimeSpan ResponseTime,
    string? Error
);
