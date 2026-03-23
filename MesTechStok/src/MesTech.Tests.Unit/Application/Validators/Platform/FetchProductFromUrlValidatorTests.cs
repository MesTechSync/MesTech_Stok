using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class FetchProductFromUrlValidatorTests
{
    private readonly FetchProductFromUrlValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new FetchProductFromUrlCommand("https://www.trendyol.com/product/12345");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyUrl_ShouldFail()
    {
        var cmd = new FetchProductFromUrlCommand("");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductUrl");
    }

    [Fact]
    public async Task UrlExceeds500Chars_ShouldFail()
    {
        var cmd = new FetchProductFromUrlCommand(new string('U', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductUrl");
    }
}
