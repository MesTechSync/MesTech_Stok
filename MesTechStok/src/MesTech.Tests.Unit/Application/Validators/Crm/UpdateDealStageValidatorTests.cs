using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
public class UpdateDealStageValidatorTests
{
    private readonly UpdateDealStageValidator _sut = new();

    private static UpdateDealStageCommand CreateValidCommand() => new(
        DealId: Guid.NewGuid(),
        NewStageId: Guid.NewGuid(),
        TenantId: Guid.NewGuid());

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
    {
        var command = CreateValidCommand();

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyDealId_ShouldFail()
    {
        var command = CreateValidCommand() with { DealId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DealId");
    }

    [Fact]
    public async Task EmptyNewStageId_ShouldFail()
    {
        var command = CreateValidCommand() with { NewStageId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewStageId");
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var command = CreateValidCommand() with { TenantId = Guid.Empty };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task AllFieldsEmpty_ShouldFail_WithThreeErrors()
    {
        var command = CreateValidCommand() with
        {
            DealId = Guid.Empty,
            NewStageId = Guid.Empty,
            TenantId = Guid.Empty
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public async Task OnlyDealId_Valid_OthersEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            NewStageId = Guid.Empty,
            TenantId = Guid.Empty
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnlyNewStageId_Valid_OthersEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            DealId = Guid.Empty,
            TenantId = Guid.Empty
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task OnlyTenantId_Valid_OthersEmpty_ShouldFail()
    {
        var command = CreateValidCommand() with
        {
            DealId = Guid.Empty,
            NewStageId = Guid.Empty
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task DealId_AndNewStageId_SameGuid_ShouldPass()
    {
        var sameGuid = Guid.NewGuid();
        var command = CreateValidCommand() with { DealId = sameGuid, NewStageId = sameGuid };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
