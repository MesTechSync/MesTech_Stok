using FluentAssertions;
using MesTech.Application.Features.System.Users;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetUsersValidatorTests
{
    private readonly GetUsersValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultQuery_ShouldProduceNoErrors()
    {
        var input = new GetUsersQuery();
        var result = await _sut.ValidateAsync(input);
        result.Errors.Should().BeEmpty();
    }

    private static GetUsersQuery CreateValidQuery() => new();
}
