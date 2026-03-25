using System.Diagnostics;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Sandbox test runner — exercises adapters against sandbox/test endpoints.
/// For each adapter: Ping (if IPingableAdapter), TestConnectionAsync, and GetCategoriesAsync.
/// No real credentials in code — sandbox credentials come from user-secrets.
/// </summary>
public sealed class SandboxTestRunner : ISandboxTestRunner
{
    private readonly IAdapterFactory _adapterFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SandboxTestRunner> _logger;

    /// <summary>
    /// Timeout for individual test operations (ping, auth, data).
    /// </summary>
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(15);

    public SandboxTestRunner(
        IAdapterFactory adapterFactory,
        IConfiguration configuration,
        ILogger<SandboxTestRunner> logger)
    {
        _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SandboxTestResult> TestAdapterAsync(string platform, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // Check if sandbox is enabled for this platform
        var sandboxSection = _configuration.GetSection($"Sandbox:{platform}");
        if (!sandboxSection.Exists())
        {
            sw.Stop();
            return new SandboxTestResult(platform, false, false, false, sw.Elapsed,
                $"No sandbox configuration found for platform '{platform}'.");
        }

        var enabled = sandboxSection.GetValue<bool>("Enabled", false);
        if (!enabled)
        {
            sw.Stop();
            return new SandboxTestResult(platform, false, false, false, sw.Elapsed,
                $"Sandbox for '{platform}' is disabled. Set Sandbox:{platform}:Enabled=true in configuration.");
        }

        var adapter = _adapterFactory.Resolve(platform);
        if (adapter is null)
        {
            sw.Stop();
            return new SandboxTestResult(platform, false, false, false, sw.Elapsed,
                $"No adapter registered for platform '{platform}'.");
        }

        var connectionOk = false;
        var authOk = false;
        var dataOk = false;
        string? error = null;

        try
        {
            // Step 1: Ping — lightweight reachability check (no credentials needed)
            connectionOk = await TestPingAsync(adapter, platform, ct).ConfigureAwait(false);

            if (!connectionOk)
            {
                sw.Stop();
                return new SandboxTestResult(platform, false, false, false, sw.Elapsed,
                    $"Platform '{platform}' endpoint is not reachable.");
            }

            // Step 2: Auth — TestConnectionAsync with empty credentials (sandbox should accept or reject gracefully)
            authOk = await TestAuthAsync(adapter, platform, ct).ConfigureAwait(false);

            // Step 3: Data — lightweight data call (GetCategoriesAsync)
            dataOk = await TestDataAsync(adapter, platform, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            error = $"Sandbox test for '{platform}' was cancelled.";
            _logger.LogWarning("Sandbox test cancelled for {Platform}", platform);
        }
        catch (Exception ex)
        {
            error = $"Unexpected error testing '{platform}': {ex.Message}";
            _logger.LogError(ex, "Unexpected error during sandbox test for {Platform}", platform);
        }

        sw.Stop();

        if (error is null && (!connectionOk || !authOk || !dataOk))
        {
            var failedSteps = new List<string>();
            if (!connectionOk) failedSteps.Add("connection");
            if (!authOk) failedSteps.Add("auth");
            if (!dataOk) failedSteps.Add("data");
            error = $"Failed steps: {string.Join(", ", failedSteps)}";
        }

        var result = new SandboxTestResult(platform, connectionOk, authOk, dataOk, sw.Elapsed, error);

        _logger.LogInformation(
            "Sandbox test for {Platform}: Connection={Connection}, Auth={Auth}, Data={Data}, Time={Time}ms, Error={Error}",
            platform, connectionOk, authOk, dataOk, sw.ElapsedMilliseconds, error ?? "(none)");

        return result;
    }

    /// <inheritdoc />
    public async Task<List<SandboxTestResult>> TestAllAsync(CancellationToken ct)
    {
        var adapters = _adapterFactory.GetAll();
        var results = new List<SandboxTestResult>(adapters.Count);

        _logger.LogInformation("Starting sandbox test for all {Count} registered adapters", adapters.Count);

        foreach (var adapter in adapters)
        {
            ct.ThrowIfCancellationRequested();
            var result = await TestAdapterAsync(adapter.PlatformCode, ct).ConfigureAwait(false);
            results.Add(result);
        }

        var successCount = results.Count(r => r.ConnectionOk && r.AuthOk && r.DataOk);
        _logger.LogInformation(
            "Sandbox test complete: {Success}/{Total} adapters passed all checks",
            successCount, results.Count);

        return results;
    }

    /// <summary>
    /// Tests endpoint reachability via IPingableAdapter.PingAsync.
    /// If adapter does not implement IPingableAdapter, assumes reachable (returns true).
    /// </summary>
    private async Task<bool> TestPingAsync(IIntegratorAdapter adapter, string platform, CancellationToken ct)
    {
        if (adapter is not IPingableAdapter pingable)
        {
            _logger.LogDebug("Adapter '{Platform}' does not implement IPingableAdapter — skipping ping, assuming reachable", platform);
            return true;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(OperationTimeout);

            var reachable = await pingable.PingAsync(cts.Token).ConfigureAwait(false);
            _logger.LogDebug("Ping for {Platform}: {Result}", platform, reachable ? "OK" : "FAIL");
            return reachable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ping failed for {Platform}", platform);
            return false;
        }
    }

    /// <summary>
    /// Tests authentication via TestConnectionAsync with empty credentials.
    /// In sandbox mode, a 401/403 from the platform still counts as "auth endpoint reachable".
    /// </summary>
    private async Task<bool> TestAuthAsync(IIntegratorAdapter adapter, string platform, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(OperationTimeout);

            // Empty credentials — sandbox should respond (even with auth error).
            // No real credentials are passed from code — user-secrets only.
            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), cts.Token).ConfigureAwait(false);

            _logger.LogDebug(
                "Auth test for {Platform}: Success={Success}, StatusCode={StatusCode}",
                platform, result.IsSuccess, result.HttpStatusCode);

            // If we got any HTTP response, the auth endpoint is functional
            return result.IsSuccess || result.HttpStatusCode.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auth test failed for {Platform}", platform);
            return false;
        }
    }

    /// <summary>
    /// Tests a lightweight data retrieval via GetCategoriesAsync.
    /// This verifies the adapter can communicate and parse responses.
    /// </summary>
    private async Task<bool> TestDataAsync(IIntegratorAdapter adapter, string platform, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(OperationTimeout);

            var categories = await adapter.GetCategoriesAsync(cts.Token).ConfigureAwait(false);

            _logger.LogDebug(
                "Data test for {Platform}: returned {Count} categories",
                platform, categories?.Count ?? 0);

            // Any response (even empty) from the data endpoint is considered success
            return categories is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Data test failed for {Platform}", platform);
            return false;
        }
    }
}
