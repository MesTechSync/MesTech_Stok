using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DeleteErpAccountMappingValidatorTests
{
    private readonly DeleteErpAccountMappingValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_Empty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task MappingId_Empty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MappingId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MappingId");
    }

    [Fact]
    public async Task BothIds_Empty_ShouldFail_WithTwoErrors()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty, MappingId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task TenantId_NewGuid_ShouldPass()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.NewGuid() };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MappingId_NewGuid_ShouldPass()
    {
        var cmd = CreateValidCommand() with { MappingId = Guid.NewGuid() };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_Empty_ErrorMessage_ShouldNotBeEmpty()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId")
            .Which.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MappingId_Empty_ErrorMessage_ShouldNotBeEmpty()
    {
        var cmd = CreateValidCommand() with { MappingId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.Errors.Should().Contain(e => e.PropertyName == "MappingId")
            .Which.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    private static DeleteErpAccountMappingCommand CreateValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid());
}
