using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateDealValidatorTests
{
    private readonly CreateDealValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyTitle_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task TitleExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task TitleExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Title = new string('A', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyPipelineId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PipelineId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PipelineId");
    }

    [Fact]
    public async Task EmptyStageId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { StageId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StageId");
    }

    [Fact]
    public async Task NegativeAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task ZeroAmount_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Amount = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateDealCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Yeni Müşteri Anlaşması",
        PipelineId: Guid.NewGuid(),
        StageId: Guid.NewGuid(),
        Amount: 10000m
    );
}
