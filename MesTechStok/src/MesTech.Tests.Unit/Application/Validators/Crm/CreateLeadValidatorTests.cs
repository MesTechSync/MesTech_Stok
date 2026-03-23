using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateLeadValidatorTests
{
    private readonly CreateLeadValidator _sut = new();

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
    public async Task EmptyFullName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FullName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public async Task FullNameExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FullName = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public async Task EmailExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Email = new string('a', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task EmailNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Email = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PhoneExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Phone = new string('1', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public async Task PhoneNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Phone = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CompanyExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Company = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Company");
    }

    [Fact]
    public async Task CompanyNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Company = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateLeadCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        FullName: "Ahmet Yılmaz",
        Source: LeadSource.Web,
        Email: "ahmet@example.com",
        Phone: "+905551234567",
        Company: "MesTech A.Ş."
    );
}
