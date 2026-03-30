using System.Diagnostics;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Settings.Commands.TestErpConnection;

/// <summary>
/// ERP baglanti testi — IErpAdapterFactory uzerinden adapter resolve edip PingAsync cagrilir.
/// Avalonia VM bu handler'i kullanarak baglanti durumunu test eder.
/// </summary>
public sealed class TestErpConnectionHandler
    : IRequestHandler<TestErpConnectionCommand, TestErpConnectionResult>
{
    private readonly IErpAdapterFactory _adapterFactory;
    private readonly ILogger<TestErpConnectionHandler> _logger;

    public TestErpConnectionHandler(
        IErpAdapterFactory adapterFactory,
        ILogger<TestErpConnectionHandler> logger)
    {
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public async Task<TestErpConnectionResult> Handle(
        TestErpConnectionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ErpProvider == ErpProvider.None)
            return TestErpConnectionResult.Failure("ERP provider secilmedi.");

        var sw = Stopwatch.StartNew();

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            var adapter = _adapterFactory.GetAdapter(request.ErpProvider);
            var isAlive = await adapter.PingAsync(cancellationToken);
            sw.Stop();

            if (isAlive)
            {
                _logger.LogInformation(
                    "ERP baglanti testi basarili: Provider={Provider}, Tenant={TenantId}, Süre={Ms}ms",
                    request.ErpProvider, request.TenantId, sw.ElapsedMilliseconds);
                return TestErpConnectionResult.Success(sw.ElapsedMilliseconds);
            }

            return TestErpConnectionResult.Failure("Baglanti testi basarisiz — ping yanit vermedi.", sw.ElapsedMilliseconds);
        }
        catch (ArgumentException)
        {
            sw.Stop();
            return TestErpConnectionResult.Failure($"Desteklenmeyen ERP provider: {request.ErpProvider}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ERP baglanti testi hatasi: Provider={Provider}", request.ErpProvider);
            return TestErpConnectionResult.Failure($"Baglanti hatasi: {ex.Message}", sw.ElapsedMilliseconds);
        }
#pragma warning restore CA1031
    }
}
