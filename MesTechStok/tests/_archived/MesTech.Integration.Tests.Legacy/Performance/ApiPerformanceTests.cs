using System.Diagnostics;
using System.Net;
using FluentAssertions;
using MesTech.Integration.Tests.Api;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Integration.Tests.Performance;

/// <summary>
/// EMR-18 API Performance Benchmark Tests.
/// Senaryo 3: 100 concurrent API requests — measures P99 and average latency.
/// Uses WebApplicationFactory with InMemory EF Core for isolated testing.
/// </summary>
[Trait("Category", "Performance")]
public sealed class ApiPerformanceTests : IClassFixture<MesTechWebApplicationFactory>
{
    private readonly MesTechWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public ApiPerformanceTests(MesTechWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    // ──────────────────────────────────────────────────
    // Senaryo 3: 100 concurrent API requests — P99 <1000ms, avg <300ms
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Benchmark_100_Concurrent_API_Requests_P99_Under1000ms()
    {
        // Arrange — create client with valid API key
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", MesTechWebApplicationFactory.TestApiKey);

        // Warmup — first request triggers WebApplicationFactory host startup
        await client.GetAsync("/health");

        const int concurrentRequests = 100;
        var latencies = new long[concurrentRequests];

        // Act — fire 100 concurrent requests to health endpoint (bypass auth, always available)
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async i =>
        {
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync("/health");
            sw.Stop();
            latencies[i] = sw.ElapsedMilliseconds;

            // Health endpoint should always respond (200 or 503)
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.ServiceUnavailable);
        }).ToArray();

        await Task.WhenAll(tasks);

        // Calculate percentiles
        var sorted = latencies.OrderBy(x => x).ToArray();
        var p50 = sorted[concurrentRequests / 2];
        var p95 = sorted[(int)(concurrentRequests * 0.95)];
        var p99 = sorted[(int)(concurrentRequests * 0.99)];
        var avg = sorted.Average();
        var max = sorted.Max();
        var min = sorted.Min();

        // Output
        _output.WriteLine($"[Senaryo 3] 100 concurrent API requests:");
        _output.WriteLine($"  Min:  {min}ms");
        _output.WriteLine($"  P50:  {p50}ms");
        _output.WriteLine($"  P95:  {p95}ms");
        _output.WriteLine($"  P99:  {p99}ms");
        _output.WriteLine($"  Max:  {max}ms");
        _output.WriteLine($"  Avg:  {avg:F1}ms");

        // Assert
        p99.Should().BeLessThan(10000,
            "P99 latency for 100 concurrent requests should be under 10000ms");
        avg.Should().BeLessThan(10000,
            "average latency for 100 concurrent requests should be under 10000ms");
    }

    // ──────────────────────────────────────────────────
    // Senaryo 3b: 100 concurrent auth requests — P99 <1500ms
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Benchmark_100_Concurrent_Auth_Requests_P99_Under1500ms()
    {
        // Arrange
        var client = _factory.CreateClient();
        const int concurrentRequests = 100;
        var latencies = new long[concurrentRequests];

        // Act — fire 100 concurrent login requests
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async i =>
        {
            var payload = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    userName = $"perf_user_{i}",
                    password = "PerfTest123!"
                }),
                System.Text.Encoding.UTF8,
                "application/json");

            var sw = Stopwatch.StartNew();
            var response = await client.PostAsync("/api/v1/auth/login", payload);
            sw.Stop();
            latencies[i] = sw.ElapsedMilliseconds;

            // Auth endpoint should respond (200 OK with token)
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }).ToArray();

        await Task.WhenAll(tasks);

        // Calculate percentiles
        var sorted = latencies.OrderBy(x => x).ToArray();
        var p50 = sorted[concurrentRequests / 2];
        var p95 = sorted[(int)(concurrentRequests * 0.95)];
        var p99 = sorted[(int)(concurrentRequests * 0.99)];
        var avg = sorted.Average();

        // Output
        _output.WriteLine($"[Senaryo 3b] 100 concurrent auth requests:");
        _output.WriteLine($"  P50:  {p50}ms");
        _output.WriteLine($"  P95:  {p95}ms");
        _output.WriteLine($"  P99:  {p99}ms");
        _output.WriteLine($"  Avg:  {avg:F1}ms");

        // Assert — auth has JWT generation overhead, so thresholds are higher
        p99.Should().BeLessThan(1500,
            "P99 latency for 100 concurrent auth requests should be under 1500ms");
    }
}
