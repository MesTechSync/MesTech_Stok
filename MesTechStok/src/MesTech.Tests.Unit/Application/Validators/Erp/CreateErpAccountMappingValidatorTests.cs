using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateErpAccountMappingValidatorTests
{
    private readonly CreateErpAccountMappingValidator _sut = new();

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
    public async Task EmptyMesTechCode_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MesTechCode = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechCode");
    }

    [Fact]
    public async Task MesTechCodeExceeds50_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MesTechCode = new string('C', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechCode");
    }

    [Fact]
    public async Task MesTechCodeExactly50_ShouldPass()
    {
        var cmd = CreateValidCommand() with { MesTechCode = new string('C', 50) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyMesTechName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MesTechName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechName");
    }

    [Fact]
    public async Task MesTechNameExceeds200_ShouldFail()
    {
        var cmd = CreateValidCommand() with { MesTechName = new string('N', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MesTechName");
    }

    [Fact]
    public async Task MesTechNameExactly200_ShouldPass()
    {
        var cmd = CreateValidCommand() with { MesTechName = new string('N', 200) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyErpCode_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpCode = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpCode");
    }

    [Fact]
    public async Task ErpCodeExceeds50_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpCode = new string('E', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpCode");
    }

    [Fact]
    public async Task EmptyErpName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpName");
    }

    [Fact]
    public async Task ErpNameExceeds200_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpName = new string('R', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpName");
    }

    [Fact]
    public async Task ErpNameExactly200_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpName = new string('R', 200) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleInvalidFields_ShouldReportAll()
    {
        var cmd = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            MesTechCode = "",
            ErpCode = "",
            ErpName = ""
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }

    private static CreateErpAccountMappingCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        MesTechCode: "MT-100",
        MesTechName: "Satis Geliri",
        MesTechType: "Revenue",
        ErpCode: "600.01",
        ErpName: "Yurtici Satis");
}
