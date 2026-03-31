using FluentAssertions;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Notifications;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetNotificationSettingsValidatorTests
{
    private readonly GetNotificationSettingsValidator _sut = new();

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

    private static GetNotificationSettingsQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), UserId: Guid.NewGuid());
}
