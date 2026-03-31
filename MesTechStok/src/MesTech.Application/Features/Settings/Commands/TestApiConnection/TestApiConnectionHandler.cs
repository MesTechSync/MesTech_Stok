using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Settings.Commands.TestApiConnection;

/// <summary>
/// API baglanti testi — verilen URL'ye HTTP GET gondererek yanit suresini olcer.
/// Avalonia SettingsVM bu handler'i kullanarak baglanti durumunu test eder.
/// </summary>
public sealed class TestApiConnectionHandler
    : IRequestHandler<TestApiConnectionCommand, TestApiConnectionResult>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TestApiConnectionHandler> _logger;

    public TestApiConnectionHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<TestApiConnectionHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TestApiConnectionResult> Handle(
        TestApiConnectionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Uri.TryCreate(request.ApiBaseUrl, UriKind.Absolute, out var uri))
            return TestApiConnectionResult.Failure("Gecersiz URL formati.");

        var sw = Stopwatch.StartNew();

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            using var client = _httpClientFactory.CreateClient("ApiConnectionTest");
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "API baglanti testi basarili: Url={Url}, Tenant={TenantId}, Sure={Ms}ms, Status={StatusCode}",
                    request.ApiBaseUrl, request.TenantId, sw.ElapsedMilliseconds, statusCode);
                return TestApiConnectionResult.Success(sw.ElapsedMilliseconds, statusCode);
            }

            return TestApiConnectionResult.Failure(
                $"Sunucu yanit verdi ancak basarisiz: HTTP {statusCode}",
                sw.ElapsedMilliseconds,
                statusCode);
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return TestApiConnectionResult.Failure("Baglanti zaman asimina ugradi (10s).", sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "API baglanti testi hatasi: Url={Url}", request.ApiBaseUrl);
            return TestApiConnectionResult.Failure($"Baglanti hatasi: {ex.Message}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "API baglanti testi beklenmeyen hata: Url={Url}", request.ApiBaseUrl);
            return TestApiConnectionResult.Failure($"Beklenmeyen hata: {ex.Message}", sw.ElapsedMilliseconds);
        }
#pragma warning restore CA1031
    }
}
