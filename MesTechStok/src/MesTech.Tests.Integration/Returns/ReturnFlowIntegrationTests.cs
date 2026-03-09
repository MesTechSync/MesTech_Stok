using System.Net.Http;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MesTech.Tests.Integration.Returns;

/// <summary>
/// İade akışı WireMock kontrat testleri — Trendyol claim endpoints.
/// 5 test: pull claims, approve, reject, empty, date filter.
/// Ciceksepeti IClaimCapableAdapter implement etmiyor (DEV 3 Dalga 5 borcu).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Flow", "Returns")]
public class ReturnFlowIntegrationTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<TrendyolAdapter> _logger;

    private const string SupplierId = "55555";

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["ApiKey"] = "test-key",
        ["ApiSecret"] = "test-secret",
        ["SupplierId"] = SupplierId
    };

    public ReturnFlowIntegrationTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new Mock<ILogger<TrendyolAdapter>>().Object;
    }

    public void Dispose() => _fixture.Reset();

    private TrendyolAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new TrendyolAdapter(httpClient, _logger);
    }

    private async Task<TrendyolAdapter> CreateConfiguredAdapterAsync()
    {
        // Stub TestConnectionAsync endpoint
        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[],""totalElements"":0,""totalPages"":0,""page"":0}"));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        return adapter;
    }

    // ════ 1. PullClaims — flat claim list ════

    [Fact]
    public async Task Trendyol_PullClaims_ReturnsFlatClaimList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/claims")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [
                        {
                            ""id"": 1001,
                            ""orderNumber"": ""ORD-001"",
                            ""status"": ""Created"",
                            ""reason"": ""DefectiveProduct"",
                            ""reasonDetail"": ""Urun kirik geldi"",
                            ""customerFirstName"": ""Ali"",
                            ""claimDate"": 1772640000000,
                            ""items"": [
                                { ""barcode"": ""8680001"", ""productName"": ""Urun A"", ""quantity"": 1, ""price"": 150.00 }
                            ]
                        },
                        {
                            ""id"": 1002,
                            ""orderNumber"": ""ORD-002"",
                            ""status"": ""Created"",
                            ""reason"": ""WrongProduct"",
                            ""customerFirstName"": ""Veli"",
                            ""claimDate"": 1772640000000,
                            ""items"": [
                                { ""barcode"": ""8680002"", ""productName"": ""Urun B"", ""quantity"": 2, ""price"": 75.00 }
                            ]
                        }
                    ],
                    ""totalPages"": 1,
                    ""totalElements"": 2,
                    ""page"": 0
                }"));

        // Act
        var claims = await adapter.PullClaimsAsync();

        // Assert
        claims.Should().HaveCount(2);
        claims[0].PlatformClaimId.Should().Be("1001");
        claims[0].OrderNumber.Should().Be("ORD-001");
        claims[0].Reason.Should().Be("DefectiveProduct");
        claims[0].Lines.Should().HaveCount(1);
        claims[0].Lines[0].Barcode.Should().Be("8680001");
        claims[1].PlatformClaimId.Should().Be("1002");
    }

    // ════ 2. ApproveClaim — success ════

    [Fact]
    public async Task Trendyol_ApproveClaim_ReturnsSuccess()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var claimId = "1001";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/claims/{claimId}/approve")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.ApproveClaimAsync(claimId);

        // Assert
        result.Should().BeTrue();
    }

    // ════ 3. RejectClaim — sends issue reason ════

    [Fact]
    public async Task Trendyol_RejectClaim_SendsIssueReason()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var claimId = "1002";

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/claims/{claimId}/issue")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.RejectClaimAsync(claimId, "WRONG_PRODUCT");

        // Assert
        result.Should().BeTrue();
    }

    // ════ 4. PullClaims — empty result ════

    [Fact]
    public async Task Trendyol_PullClaims_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/claims")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""content"":[],""totalPages"":0,""totalElements"":0,""page"":0}"));

        // Act
        var claims = await adapter.PullClaimsAsync();

        // Assert
        claims.Should().BeEmpty();
    }

    // ════ 5. PullClaims — date filter sends epoch ════

    [Fact]
    public async Task Trendyol_PullClaims_WithDateFilter_SendsEpochTimestamp()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        var since = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEpoch = new DateTimeOffset(since).ToUnixTimeMilliseconds().ToString();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/sapigw/suppliers/{SupplierId}/claims")
                .WithParam("claimDate", expectedEpoch)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [
                        {
                            ""id"": 2001,
                            ""orderNumber"": ""ORD-FILTERED"",
                            ""status"": ""Created"",
                            ""reason"": ""LateDelivery"",
                            ""customerFirstName"": ""Ayse"",
                            ""claimDate"": 1772640000000,
                            ""items"": []
                        }
                    ],
                    ""totalPages"": 1,
                    ""totalElements"": 1,
                    ""page"": 0
                }"));

        // Act
        var claims = await adapter.PullClaimsAsync(since);

        // Assert
        claims.Should().HaveCount(1);
        claims[0].OrderNumber.Should().Be("ORD-FILTERED");
    }
}
