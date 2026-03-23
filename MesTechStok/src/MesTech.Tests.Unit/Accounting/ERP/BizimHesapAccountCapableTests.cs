using System.Net;
using FluentAssertions;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// BizimHesapERPAdapter IErpAccountCapable contract tests.
/// Tests: CreateAccount, GetAccount, UpdateAccount, SearchAccounts, GetAccountBalance.
/// </summary>
[Trait("Category", "Unit")]
public class BizimHesapAccountCapableTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly IErpAccountCapable _sut;

    public BizimHesapAccountCapableTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.bizimhesap.com")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:BizimHesap:BaseUrl"] = "https://api.bizimhesap.com/v1",
                ["ERP:BizimHesap:ApiKey"] = "test-api-key-bh"
            })
            .Build();

        var apiClient = new BizimHesapApiClient(
            httpClient,
            config,
            new Mock<ILogger<BizimHesapApiClient>>().Object);

        _sut = new BizimHesapERPAdapter(
            apiClient,
            new Mock<ILogger<BizimHesapERPAdapter>>().Object);
    }

    private void SetupHttpResponse(HttpStatusCode status, string content)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content)
            });
    }

    private static ErpAccountRequest MakeAccountRequest() => new(
        AccountCode: "120.01.001",
        CompanyName: "Test Ticaret A.S.",
        TaxId: "1234567890",
        TaxOffice: "Kadikoy VD",
        Address: "Istanbul, Turkey",
        City: "Istanbul",
        Phone: "+905551234567",
        Email: "info@test.com"
    );

    // ── CreateAccount ──

    [Fact]
    public async Task CreateAccount_Success_ReturnsOkResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"{""code"":""120.01.001"",""name"":""Test Ticaret A.S."",""balance"":""0.00""}");

        // Act
        var result = await _sut.CreateAccountAsync(MakeAccountRequest());

        // Assert
        result.Success.Should().BeTrue();
        result.AccountCode.Should().Be("120.01.001");
        result.AccountName.Should().Be("Test Ticaret A.S.");
    }

    [Fact]
    public async Task CreateAccount_Conflict_ReturnsFailedResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Conflict, @"{""error"":""account already exists""}");

        // Act
        var result = await _sut.CreateAccountAsync(MakeAccountRequest());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAccount_NullRequest_ThrowsArgumentNull()
    {
        var act = () => _sut.CreateAccountAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetAccount ──

    [Fact]
    public async Task GetAccount_Found_ReturnsResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"{""code"":""120.01.001"",""name"":""Test Ticaret"",""balance"":""5250.75""}");

        // Act
        var result = await _sut.GetAccountAsync("1234567890");

        // Assert
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccountCode.Should().Be("120.01.001");
        result.Balance.Should().Be(5250.75m);
    }

    [Fact]
    public async Task GetAccount_NotFound_ReturnsNull()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, @"{""error"":""not found""}");

        // Act
        var result = await _sut.GetAccountAsync("9999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccount_NullInput_ThrowsArgumentException()
    {
        var act = () => _sut.GetAccountAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── UpdateAccount ──

    [Fact]
    public async Task UpdateAccount_Success_ReturnsOkResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"{""code"":""120.01.001"",""name"":""Updated Ticaret"",""balance"":""1000.00""}");

        // Act
        var result = await _sut.UpdateAccountAsync(MakeAccountRequest());

        // Assert
        result.Success.Should().BeTrue();
        result.AccountName.Should().Be("Updated Ticaret");
        result.Balance.Should().Be(1000m);
    }

    [Fact]
    public async Task UpdateAccount_NotFound_ReturnsFailedResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, @"{""error"":""contact not found""}");

        // Act
        var result = await _sut.UpdateAccountAsync(MakeAccountRequest());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ── SearchAccounts ──

    [Fact]
    public async Task SearchAccounts_MatchesFound_ReturnsList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"[{""code"":""120.01.001"",""name"":""ABC Tic."",""balance"":""100.00""},{""code"":""120.01.002"",""name"":""ABC Ltd."",""balance"":""200.00""}]");

        // Act
        var result = await _sut.SearchAccountsAsync("ABC");

        // Assert
        result.Should().HaveCount(2);
        result[0].AccountCode.Should().Be("120.01.001");
        result[1].Balance.Should().Be(200m);
    }

    [Fact]
    public async Task SearchAccounts_NoMatch_ReturnsEmpty()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, @"[]");

        // Act
        var result = await _sut.SearchAccountsAsync("NONEXISTENT");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAccounts_NullQuery_ThrowsArgumentException()
    {
        var act = () => _sut.SearchAccountsAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetAccountBalance ──

    [Fact]
    public async Task GetAccountBalance_Success_ReturnsDecimal()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, @"{""balance"":""12345.67""}");

        // Act
        var result = await _sut.GetAccountBalanceAsync("120.01.001");

        // Assert
        result.Should().Be(12345.67m);
    }

    [Fact]
    public async Task GetAccountBalance_ServerError_ReturnsZero()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "error");

        // Act
        var result = await _sut.GetAccountBalanceAsync("120.01.001");

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task GetAccountBalance_NullInput_ThrowsArgumentException()
    {
        var act = () => _sut.GetAccountBalanceAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
