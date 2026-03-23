using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ReplyToMessageValidatorTests
{
    private readonly ReplyToMessageValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyMessageId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MessageId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MessageId");
    }

    [Fact]
    public async Task EmptyReply_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Reply = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reply");
    }

    [Fact]
    public async Task ReplyExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Reply = new string('X', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reply");
    }

    [Fact]
    public async Task EmptyRepliedBy_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RepliedBy = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RepliedBy");
    }

    [Fact]
    public async Task RepliedByExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RepliedBy = new string('U', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RepliedBy");
    }

    private static ReplyToMessageCommand CreateValidCommand() => new(
        MessageId: Guid.NewGuid(),
        Reply: "Teşekkürler, inceliyoruz.",
        RepliedBy: "admin@mestech.com"
    );
}
