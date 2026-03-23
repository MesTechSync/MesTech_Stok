using FluentAssertions;
using MesTech.Application.Features.Logging.Queries.GetLogs;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Logging;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetLogsValidatorTests
{
    private readonly GetLogsValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetLogsQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var query = new GetLogsQuery(Guid.Empty);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Page_WhenZero_ShouldFail()
    {
        var query = new GetLogsQuery(Guid.NewGuid(), Page: 0);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public async Task Page_WhenNegative_ShouldFail()
    {
        var query = new GetLogsQuery(Guid.NewGuid(), Page: -1);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public async Task PageSize_WhenZero_ShouldFail()
    {
        var query = new GetLogsQuery(Guid.NewGuid(), PageSize: 0);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public async Task PageSize_WhenExceeds500_ShouldFail()
    {
        var query = new GetLogsQuery(Guid.NewGuid(), PageSize: 501);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public async Task PageSize_WhenExactly500_ShouldPass()
    {
        var query = new GetLogsQuery(Guid.NewGuid(), PageSize: 500);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PageSize_WhenExactly1_ShouldPass()
    {
        var query = new GetLogsQuery(Guid.NewGuid(), PageSize: 1);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
