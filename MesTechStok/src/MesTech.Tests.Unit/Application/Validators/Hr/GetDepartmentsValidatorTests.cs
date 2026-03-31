using FluentAssertions;
using MesTech.Application.Features.Hr.Queries.GetDepartments;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Hr;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetDepartmentsValidatorTests
{
    private readonly GetDepartmentsValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var input = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static GetDepartmentsQuery CreateValidQuery() => new(TenantId: Guid.NewGuid());
}
