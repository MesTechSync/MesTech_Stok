using FluentAssertions;
using MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Product;

public class FetchProductFromPlatformValidatorTests
{
    private readonly FetchProductFromPlatformValidator _sut = new();

    private static FetchProductFromPlatformQuery CreateValidQuery() => new(
        ProductUrl: "https://www.trendyol.com/product/12345");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidHttpsUrl_ShouldPass()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ValidHttpUrl_ShouldPass()
    {
        var query = CreateValidQuery() with { ProductUrl = "http://example.com/product/1" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [Trait("Category", "Unit")]
    public async Task EmptyOrNullUrl_ShouldFail(string? url)
    {
        var query = CreateValidQuery() with { ProductUrl = url! };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductUrl");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InvalidUrl_ShouldFail()
    {
        var query = CreateValidQuery() with { ProductUrl = "not-a-url" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductUrl");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FtpUrl_ShouldFail()
    {
        var query = CreateValidQuery() with { ProductUrl = "ftp://files.example.com/product.csv" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductUrl");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HttpsUrlWithPath_ShouldPass()
    {
        var query = CreateValidQuery() with { ProductUrl = "https://www.n11.com/urun/test-product-123456" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HttpsUrlWithQueryString_ShouldPass()
    {
        var query = CreateValidQuery() with { ProductUrl = "https://www.hepsiburada.com/product?id=999&ref=search" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PlainText_ShouldFail()
    {
        var query = CreateValidQuery() with { ProductUrl = "just some text" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
    }
}
