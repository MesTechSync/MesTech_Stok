using FluentAssertions;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetUnreadNotificationCountValidatorTests
{
    private readonly GetUnreadNotificationCountValidator _sut = new();

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

    [Fact]
    public async Task EmptyUserId_ShouldFail()
    {
        var input = CreateValidQuery() with { UserId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    private static GetUnreadNotificationCountQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), UserId: Guid.NewGuid());
}
