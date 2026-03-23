using FluentAssertions;
using MesTech.Application.Commands.ProcessAiRecommendation;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ProcessAiRecommendationValidatorTests
{
    private readonly ProcessAiRecommendationValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RecommendationType_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecommendationType = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecommendationType");
    }

    [Fact]
    public async Task RecommendationType_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RecommendationType = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecommendationType");
    }

    [Fact]
    public async Task Title_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Title_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Title = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task Description_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Description_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Description = new string('D', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Priority_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Priority = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    [Fact]
    public async Task Priority_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Priority = new string('P', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static ProcessAiRecommendationCommand CreateValidCommand() => new()
    {
        RecommendationType = "PriceOptimization",
        Title = "Reduce price for SKU-001",
        Description = "AI suggests reducing price by 10% to increase sales velocity",
        Priority = "High",
        TenantId = Guid.NewGuid()
    };
}
