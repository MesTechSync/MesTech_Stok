using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class LoseDealValidatorTests
{
    private readonly LoseDealValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new LoseDealCommand(Guid.NewGuid(), "Fiyat uyuşmazlığı");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyDealId_ShouldFail()
    {
        var cmd = new LoseDealCommand(Guid.Empty, "Fiyat uyuşmazlığı");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DealId");
    }

    [Fact]
    public async Task EmptyReason_ShouldFail()
    {
        var cmd = new LoseDealCommand(Guid.NewGuid(), "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task ReasonExceeds500Chars_ShouldFail()
    {
        var cmd = new LoseDealCommand(Guid.NewGuid(), new string('R', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task ReasonExactly500Chars_ShouldPass()
    {
        var cmd = new LoseDealCommand(Guid.NewGuid(), new string('R', 500));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
