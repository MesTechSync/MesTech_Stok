using FluentAssertions;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Calendar;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetCalendarEventByIdValidatorTests
{
    private readonly GetCalendarEventByIdValidator _sut = new();

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

    private static GetCalendarEventByIdQuery CreateValidQuery() => new(Id: Guid.NewGuid());
}
