using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Security;

/// <summary>
/// DeletePersonalDataValidator: KVKK veri silme komutu doğrulaması.
/// Kritik: Reason zorunlu — yasal iz bırakma gereksinimi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
[Trait("Group", "KvkkCompliance")]
public class DeletePersonalDataValidatorTests
{
    private readonly DeletePersonalDataValidator _validator = new();

    private static DeletePersonalDataCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        RequestedByUserId: Guid.NewGuid(),
        Reason: "KVKK Madde 7 — kullanıcı talebi üzerine veri silme");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_RequestedByUserId_Fails()
    {
        var cmd = ValidCommand() with { RequestedByUserId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RequestedByUserId");
    }

    [Fact]
    public void Empty_Reason_Fails()
    {
        // KVKK uyumu: sebep belirtmeden veri silinemez
        var cmd = ValidCommand() with { Reason = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Reason_Over500_Fails()
    {
        var cmd = ValidCommand() with { Reason = new string('R', 501) };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void Reason_Exactly500_Passes()
    {
        var cmd = ValidCommand() with { Reason = new string('R', 500) };
        var result = _validator.Validate(cmd);
        result.Errors.Should().NotContain(e => e.PropertyName == "Reason");
    }
}
