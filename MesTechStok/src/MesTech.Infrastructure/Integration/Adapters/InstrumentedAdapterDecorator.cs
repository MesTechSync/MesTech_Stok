using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Decorator that wraps any IIntegratorAdapter with Prometheus metrics instrumentation.
/// Records mestech_adapter_api_calls_total and mestech_adapter_api_duration_seconds
/// for every adapter method call (PushProduct, PullProducts, PushStock, PushPrice, GetCategories).
///
/// Usage: Register in DI as a wrapping layer around the real adapter.
/// This avoids modifying 23 individual adapter files.
/// </summary>
public sealed class InstrumentedAdapterDecorator : IIntegratorAdapter, IPingableAdapter
{
    private readonly IIntegratorAdapter _inner;
    private readonly ILogger<InstrumentedAdapterDecorator> _logger;

    public string PlatformCode => _inner.PlatformCode;
    public bool SupportsStockUpdate => _inner.SupportsStockUpdate;
    public bool SupportsPriceUpdate => _inner.SupportsPriceUpdate;
    public bool SupportsShipment => _inner.SupportsShipment;

    public InstrumentedAdapterDecorator(IIntegratorAdapter inner, ILogger<InstrumentedAdapterDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => InstrumentAsync("PushProduct", () => _inner.PushProductAsync(product, ct));

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => InstrumentAsync("PullProducts", () => _inner.PullProductsAsync(ct));

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => InstrumentAsync("PushStock", () => _inner.PushStockUpdateAsync(productId, newStock, ct));

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => InstrumentAsync("PushPrice", () => _inner.PushPriceUpdateAsync(productId, newPrice, ct));

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
        => InstrumentAsync("TestConnection", () => _inner.TestConnectionAsync(credentials, ct));

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => InstrumentAsync("GetCategories", () => _inner.GetCategoriesAsync(ct));

    public Task<bool> PingAsync(CancellationToken ct = default)
    {
        if (_inner is IPingableAdapter pingable)
            return InstrumentAsync("Ping", () => pingable.PingAsync(ct));

        return Task.FromResult(false);
    }

    private async Task<T> InstrumentAsync<T>(string method, Func<Task<T>> action)
    {
        var platform = _inner.PlatformCode.ToLowerInvariant();
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action().ConfigureAwait(false);
            sw.Stop();

            AdapterMetrics.ApiCallsTotal.WithLabels(platform, method, "success").Inc();
            AdapterMetrics.ApiCallDuration.WithLabels(platform, method).Observe(sw.Elapsed.TotalSeconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            AdapterMetrics.ApiCallsTotal.WithLabels(platform, method, "timeout").Inc();
            AdapterMetrics.ApiCallDuration.WithLabels(platform, method).Observe(sw.Elapsed.TotalSeconds);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            AdapterMetrics.ApiCallsTotal.WithLabels(platform, method, "error").Inc();
            AdapterMetrics.ApiCallDuration.WithLabels(platform, method).Observe(sw.Elapsed.TotalSeconds);

            _logger.LogWarning(ex, "[InstrumentedAdapter] {Platform}.{Method} failed after {Duration}ms",
                platform, method, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
