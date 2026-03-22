using System.Net;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Soap;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// SimpleSoapClient unit testleri.
/// HTTP transport, SOAP envelope parse, Fault detection.
/// Contributes to Infrastructure layer coverage.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "SimpleSoapClient")]
[Trait("Phase", "Dalga5")]
public class SimpleSoapClientTests
{
    private static readonly XNamespace SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
    private readonly ILogger _logger = new Mock<ILogger>().Object;

    private static HttpClient BuildHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    private static string BuildSoapResponse(string innerBodyXml) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
          <soapenv:Header/>
          <soapenv:Body>
            {innerBodyXml}
          </soapenv:Body>
        </soapenv:Envelope>
        """;

    // ── Constructor Guards ──

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        var act = () => new SimpleSoapClient(null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SimpleSoapClient(new HttpClient(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ── SendAsync ──

    [Fact]
    public async Task SendAsync_ValidResponse_ReturnsParsedBodyElement()
    {
        var responseXml = BuildSoapResponse("<TestResponse><Value>42</Value></TestResponse>");
        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseXml)
        });
        var client = new SimpleSoapClient(httpClient, _logger);
        var body = new XElement("TestRequest");

        var result = await client.SendAsync("http://localhost/soap", "TestAction", body);

        result.Name.LocalName.Should().Be("TestResponse");
        result.Element("Value")!.Value.Should().Be("42");
    }

    [Fact]
    public async Task SendAsync_Non2xxStatus_ThrowsHttpRequestException()
    {
        // Use 400 BadRequest to avoid Polly retry delays (Polly only retries >= 500 and 429)
        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request")
        });
        var client = new SimpleSoapClient(httpClient, _logger);
        var body = new XElement("TestRequest");

        var act = () => client.SendAsync("http://localhost/soap", "TestAction", body);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_MissingBodyElement_ThrowsInvalidOperationException()
    {
        // Response has no soapenv:Body
        const string malformed = """
            <?xml version="1.0"?>
            <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">
              <soapenv:Header/>
            </soapenv:Envelope>
            """;
        var httpClient = BuildHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(malformed)
        });
        var client = new SimpleSoapClient(httpClient, _logger);
        var body = new XElement("TestRequest");

        var act = () => client.SendAsync("http://localhost/soap", "TestAction", body);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── ThrowIfFault ──

    [Fact]
    public void ThrowIfFault_WithSoapFault_ThrowsInvalidOperationExceptionWithMessage()
    {
        const string faultXml = """
            <root>
              <Fault>
                <faultcode>Server</faultcode>
                <faultstring>Kimlik dogrulanamadi</faultstring>
              </Fault>
            </root>
            """;
        var body = XElement.Parse(faultXml);

        var act = () => SimpleSoapClient.ThrowIfFault(body);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Kimlik dogrulanamadi*");
    }

    [Fact]
    public void ThrowIfFault_NoFault_DoesNotThrow()
    {
        const string cleanXml = "<TestResponse><Value>OK</Value></TestResponse>";
        var body = XElement.Parse(cleanXml);

        var act = () => SimpleSoapClient.ThrowIfFault(body);

        act.Should().NotThrow();
    }
}
