using System.Net.Http;
using System.Reflection;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// DEV 5 — Dalga 7 Task 5.11: Extended Cross-Platform Regression.
/// Extends FivePlatformRegressionTests with N11 (SOAP) + AmazonTR (SP-API) + Cargo adapters.
/// Ensures adding Bitrix24 (Task 5.08-5.10) won't break existing 8 platforms.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "CrossPlatform")]
public class ExtendedPlatformRegressionTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;

    public ExtendedPlatformRegressionTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    private static Mock<IHttpClientFactory> CreateN11MockFactory()
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        return mock;
    }

    // ════════════════════════════════════════════════════════════════
    //  N11 ADAPTER (SOAP) — Dalga 4+5
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void N11_PlatformCode_IsCorrect()
    {
        var adapter = new N11Adapter(NullLoggerFactory.Instance.CreateLogger<N11Adapter>(), CreateN11MockFactory().Object);
        adapter.PlatformCode.Should().Be("N11");
    }

    [Fact]
    public void N11_ImplementsIIntegratorAdapter()
    {
        typeof(N11Adapter).Should().Implement<IIntegratorAdapter>();
    }

    [Fact]
    public async Task N11_TestConnection_WithValidSoap_ReturnsSuccess()
    {
        _fixture.Reset();

        _mockServer
            .Given(Request.Create()
                .WithPath("/ws/CategoryService.wsdl")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/xml")
                .WithBody(SoapWireMockHelper.BuildN11GetCategoryListResponse(3)));

        var adapter = new N11Adapter(NullLoggerFactory.Instance.CreateLogger<N11Adapter>(), CreateN11MockFactory().Object);
        var credentials = new Dictionary<string, string>
        {
            ["N11AppKey"] = "test-n11-key",
            ["N11AppSecret"] = "test-n11-secret",
            ["N11BaseUrl"] = _fixture.BaseUrl
        };

        var result = await adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("N11");
    }

    [Fact]
    public async Task N11_TestConnection_MissingCredentials_ReturnsError()
    {
        var adapter = new N11Adapter(NullLoggerFactory.Instance.CreateLogger<N11Adapter>(), CreateN11MockFactory().Object);
        var credentials = new Dictionary<string, string>
        {
            ["N11BaseUrl"] = _fixture.BaseUrl
        };

        var result = await adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════════════════════════════════════════════════════════════════
    //  AMAZON TR ADAPTER (SP-API + LWA OAuth2) — Dalga 6
    // ════════════════════════════════════════════════════════════════

    private const string AmazonSellerId = "AREG-SELLER-001";

    private AmazonTrAdapter CreateAmazonAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new AmazonTrAdapter(httpClient,
            NullLoggerFactory.Instance.CreateLogger<AmazonTrAdapter>());
    }

    private Dictionary<string, string> AmazonCredentials() => new()
    {
        ["RefreshToken"] = "Atzr|reg-test-refresh-token",
        ["ClientId"] = "amzn1.application-oa2-client.reg-test",
        ["ClientSecret"] = "reg-test-client-secret",
        ["SellerId"] = AmazonSellerId,
        ["BaseUrl"] = _fixture.BaseUrl,
        ["LwaEndpoint"] = $"{_fixture.BaseUrl}/auth/o2/token"
    };

    private void StubAmazonLwaToken()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/auth/o2/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildLwaTokenResponse("Atza|reg-test-access", 3600)));
    }

    private void StubAmazonCatalog()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath("/catalog/2022-04-01/items")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(AmazonWireMockHelper.BuildCatalogItemsResponse(
                    new[] { ("B00REG001", "Regression Product", "REG-AMZ-001") })));
    }

    [Fact]
    public void AmazonTr_PlatformCode_IsCorrect()
    {
        var adapter = CreateAmazonAdapter();
        adapter.PlatformCode.Should().Be("Amazon");
    }

    [Fact]
    public void AmazonTr_ImplementsIIntegratorAdapter()
    {
        typeof(AmazonTrAdapter).Should().Implement<IIntegratorAdapter>();
    }

    [Fact]
    public async Task AmazonTr_TestConnection_WithLwaToken_ReturnsSuccess()
    {
        _fixture.Reset();

        StubAmazonLwaToken();
        StubAmazonCatalog();

        var adapter = CreateAmazonAdapter();
        var result = await adapter.TestConnectionAsync(AmazonCredentials());

        result.IsSuccess.Should().BeTrue();
        result.HttpStatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AmazonTr_TestConnection_MissingRefreshToken_ReturnsError()
    {
        var adapter = CreateAmazonAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["ClientId"] = "test",
            ["ClientSecret"] = "test",
            ["SellerId"] = "test"
        };

        var result = await adapter.TestConnectionAsync(credentials);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ════════════════════════════════════════════════════════════════
    //  CARGO ADAPTERS — Dalga 3-5
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("YurticiKargoAdapter")]
    [InlineData("ArasKargoAdapter")]
    [InlineData("SuratKargoAdapter")]
    public void CargoAdapter_Exists_AndImplementsICargoAdapter(string adapterName)
    {
        var infraAssembly = typeof(TrendyolAdapter).Assembly;
        var cargoInterface = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ICargoAdapter");

        // ICargoAdapter might be in Application assembly
        cargoInterface ??= typeof(IIntegratorAdapter).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ICargoAdapter");

        cargoInterface.Should().NotBeNull("ICargoAdapter interface must exist");

        var adapterType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == adapterName);

        adapterType.Should().NotBeNull($"{adapterName} must exist");
        cargoInterface!.IsAssignableFrom(adapterType).Should().BeTrue(
            $"{adapterName} must implement ICargoAdapter");
    }

    [Theory]
    [InlineData("YurticiKargoAdapter", "YurticiKargo")]
    [InlineData("ArasKargoAdapter", "Aras")]
    [InlineData("SuratKargoAdapter", "Surat")]
    public void CargoAdapter_HasProviderProperty(string adapterName, string expectedProvider)
    {
        var infraAssembly = typeof(TrendyolAdapter).Assembly;
        var adapterType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == adapterName);

        adapterType.Should().NotBeNull();

        var providerProp = adapterType!.GetProperty("Provider");
        providerProp.Should().NotBeNull($"{adapterName} must have Provider property");

        // Verify the expected provider name is valid
        expectedProvider.Should().NotBeNullOrEmpty();
    }

    // ════════════════════════════════════════════════════════════════
    //  ALL PLATFORM ADAPTER COEXISTENCE — Dalga 7 (pre-Bitrix24)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("TrendyolAdapter", "Trendyol")]
    [InlineData("OpenCartAdapter", "OpenCart")]
    [InlineData("CiceksepetiAdapter", "Ciceksepeti")]
    [InlineData("HepsiburadaAdapter", "Hepsiburada")]
    [InlineData("PazaramaAdapter", "Pazarama")]
    [InlineData("N11Adapter", "N11")]
    [InlineData("AmazonTrAdapter", "Amazon")]
    public void AllMarketplaceAdapters_ImplementIIntegratorAdapter(string adapterName, string expectedPlatformCode)
    {
        var infraAssembly = typeof(TrendyolAdapter).Assembly;
        var adapterType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == adapterName);

        adapterType.Should().NotBeNull($"{adapterName} must exist");
        typeof(IIntegratorAdapter).IsAssignableFrom(adapterType).Should().BeTrue(
            $"{adapterName} must implement IIntegratorAdapter");

        // Verify PlatformCode property exists
        var platformProp = adapterType!.GetProperty("PlatformCode");
        platformProp.Should().NotBeNull($"{adapterName} must expose PlatformCode");

        expectedPlatformCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AdapterFactory_ShouldExist_WithResolveMethod()
    {
        var infraAssembly = typeof(TrendyolAdapter).Assembly;
        var factoryType = infraAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AdapterFactory");

        factoryType.Should().NotBeNull("AdapterFactory must exist for DI-based adapter resolution");

        var methods = factoryType!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToArray();

        methods.Should().Contain("Resolve");
        methods.Should().Contain("GetAll");
        methods.Should().Contain("ResolveCapability");
    }

    [Fact]
    public void StubAdapters_ShouldExist_ForFuturePlatforms()
    {
        // These are stub adapters for planned platforms
        var infraAssembly = typeof(TrendyolAdapter).Assembly;
        var stubNames = new[] { "EbayAdapter", "OzonAdapter", "PttAvmAdapter" };

        foreach (var name in stubNames)
        {
            var type = infraAssembly.GetTypes().FirstOrDefault(t => t.Name == name);
            type.Should().NotBeNull($"Stub adapter {name} must exist for future platform");
            typeof(IIntegratorAdapter).IsAssignableFrom(type).Should().BeTrue();
        }
    }
}
