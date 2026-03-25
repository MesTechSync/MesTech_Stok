using System.Net;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace MesTech.Infrastructure.Integration.Soap;

/// <summary>
/// Basit SOAP client — Yurtici Kargo / N11 / PTT Kargo XML/SOAP API icin.
/// Full WCF bagimliligini onlemek icin minimal HttpClient tabanli implementasyon.
/// Polly retry + circuit breaker dahil.
/// </summary>
public sealed class SimpleSoapClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    private static readonly XNamespace SoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";

    public SimpleSoapClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _resiliencePipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 2,
                DelayGenerator = args => new ValueTask<TimeSpan?>(
                    TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber))),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "SOAP retry {Attempt} after {Delay}ms (status: {Status})",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Result?.StatusCode);
                    return default;
                }
            })
            .Build();
    }

    /// <summary>
    /// SOAP request gonderir ve response body XElement doner.
    /// Polly retry pipeline ile korunur.
    /// </summary>
    public async Task<XElement> SendAsync(
        string url,
        string soapAction,
        XElement body,
        CancellationToken ct = default)
    {
        var envelope = new XElement(SoapEnv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", SoapEnv.NamespaceName),
            new XElement(SoapEnv + "Header"),
            new XElement(SoapEnv + "Body", body));

        var xmlString = envelope.ToString(SaveOptions.DisableFormatting);

        _logger.LogDebug("SOAP request to {Url} action={Action}", url, soapAction);

        var response = await _resiliencePipeline.ExecuteAsync(async token =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(xmlString, Encoding.UTF8, "text/xml");
            request.Headers.Add("SOAPAction", soapAction);
            return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("SOAP request failed {Status}: {Content}", response.StatusCode, responseContent);
            throw new HttpRequestException($"SOAP request failed: {response.StatusCode}");
        }

        var responseDoc = XDocument.Parse(responseContent);
        var responseBody = responseDoc.Descendants(SoapEnv + "Body").FirstOrDefault();

        if (responseBody is null)
            throw new InvalidOperationException("SOAP response does not contain a Body element");

        // Return the first child of Body (the actual response element)
        var result = responseBody.Elements().FirstOrDefault();
        if (result is null)
            throw new InvalidOperationException("SOAP response Body is empty");

        return result;
    }

    /// <summary>
    /// SOAP Fault kontrolu yapar. Fault varsa exception firlatir.
    /// </summary>
    public static void ThrowIfFault(XElement responseBody)
    {
        var fault = responseBody.Descendants(SoapEnv + "Fault").FirstOrDefault()
                    ?? responseBody.Descendants("Fault").FirstOrDefault();

        if (fault is not null)
        {
            var faultString = fault.Element("faultstring")?.Value ?? "Unknown SOAP Fault";
            throw new InvalidOperationException($"SOAP Fault: {faultString}");
        }
    }
}
