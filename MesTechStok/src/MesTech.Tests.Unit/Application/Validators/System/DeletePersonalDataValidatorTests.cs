using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.System;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeletePersonalDataValidatorTests
{
    private readonly DeletePersonalDataValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "KVKK talep");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new DeletePersonalDataCommand(Guid.Empty, Guid.NewGuid(), "KVKK talep");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyRequestedByUserId_ShouldFail()
    {
        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.Empty, "KVKK talep");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequestedByUserId");
    }

    [Fact]
    public async Task EmptyReason_ShouldFail()
    {
        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task ReasonExceeds500Chars_ShouldFail()
    {
        var cmd = new DeletePersonalDataCommand(Guid.NewGuid(), Guid.NewGuid(), new string('R', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}
