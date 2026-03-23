using FluentAssertions;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Logging;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetLogCountValidatorTests
{
    private readonly GetLogCountValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = new GetLogCountQuery(Guid.NewGuid());
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidQuery_WithCategory_ShouldPass()
    {
        var query = new GetLogCountQuery(Guid.NewGuid(), "StockSync");
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var query = new GetLogCountQuery(Guid.Empty);
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
