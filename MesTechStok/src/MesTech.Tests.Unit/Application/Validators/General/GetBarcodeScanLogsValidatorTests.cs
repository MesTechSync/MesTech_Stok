using FluentAssertions;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.General;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetBarcodeScanLogsValidatorTests
{
    private readonly GetBarcodeScanLogsValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Page_WhenZero_ShouldFail()
    {
        var input = CreateValidQuery() with { Page = 0 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task PageSize_WhenZero_ShouldFail()
    {
        var input = CreateValidQuery() with { PageSize = 0 };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
    }

    private static GetBarcodeScanLogsQuery CreateValidQuery() => new(Page: 1, PageSize: 20, BarcodeFilter: "test", SourceFilter: "test");
}
