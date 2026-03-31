using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetPenaltyRecordByIdValidatorTests
{
    private readonly GetPenaltyRecordByIdValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_ShouldFail()
    {
        var input = CreateValidQuery() with { Id = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    private static GetPenaltyRecordByIdQuery CreateValidQuery() => new(Id: Guid.NewGuid());
}
