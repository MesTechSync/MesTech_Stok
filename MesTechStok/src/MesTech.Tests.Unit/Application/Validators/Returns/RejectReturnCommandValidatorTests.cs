using FluentAssertions;
using MesTech.Application.Commands.RejectReturn;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Returns;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RejectReturnCommandValidatorTests
{
    private readonly RejectReturnCommandValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnRequestId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ReturnRequestId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnRequestId");
    }

    [Fact]
    public async Task RejectionReason_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RejectionReason = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RejectionReason");
    }

    [Fact]
    public async Task RejectionReason_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { RejectionReason = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RejectionReason_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { RejectionReason = new string('R', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static RejectReturnCommand CreateValidCommand() => new(
        ReturnRequestId: Guid.NewGuid(),
        RejectionReason: "Urun hasarli degil"
    );
}
