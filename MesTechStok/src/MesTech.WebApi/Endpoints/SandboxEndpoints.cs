using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Admin-only sandbox test endpoints — verify adapter connectivity against sandbox/test environments.
/// Demir Kural 4: Sandbox ZORUNLU — production'a test verisi GITMEZ.
/// </summary>
public static class SandboxEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/sandbox-test")
            .WithTags("Sandbox (Admin)")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/admin/sandbox-test/all — test all registered adapters
        group.MapGet("/all", async (ISandboxTestRunner runner, CancellationToken ct) =>
        {
            var results = await runner.TestAllAsync(ct);
            return Results.Ok(new SandboxTestAllResponse(
                DateTime.UtcNow,
                results.Count,
                results.Count(r => r.ConnectionOk && r.AuthOk && r.DataOk),
                results.Select(r => new SandboxTestResultItem(
                    r.Platform, r.ConnectionOk, r.AuthOk, r.DataOk,
                    Math.Round(r.ResponseTime.TotalMilliseconds, 1), r.Error
                )).ToList()));
        })
        .WithName("TestAllSandboxAdapters")
        .WithSummary("Test all registered adapters against sandbox endpoints").Produces(200).Produces(400);

        // GET /api/v1/admin/sandbox-test/{platform} — test a single adapter
        group.MapGet("/{platform}", async (string platform, ISandboxTestRunner runner, CancellationToken ct) =>
        {
            var result = await runner.TestAdapterAsync(platform, ct);
            return Results.Ok(new SandboxTestSingleResponse(
                DateTime.UtcNow,
                result.Platform, result.ConnectionOk, result.AuthOk, result.DataOk,
                Math.Round(result.ResponseTime.TotalMilliseconds, 1), result.Error));
        })
        .WithName("TestSandboxAdapter")
        .WithSummary("Test a single adapter against its sandbox endpoint").Produces(200).Produces(400);
    }

    public sealed record SandboxTestResultItem(
        string Platform, bool ConnectionOk, bool AuthOk, bool DataOk,
        double ResponseTimeMs, string? Error);

    public sealed record SandboxTestAllResponse(
        DateTime Timestamp, int TotalAdapters, int Passed,
        IReadOnlyList<SandboxTestResultItem> Results);

    public sealed record SandboxTestSingleResponse(
        DateTime Timestamp, string Platform, bool ConnectionOk, bool AuthOk,
        bool DataOk, double ResponseTimeMs, string? Error);
}
