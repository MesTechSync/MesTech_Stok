using FluentAssertions;
using MesTech.Application.Commands.CreateCustomer;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Customers;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateCustomerValidatorTests
{
    private readonly CreateCustomerValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValid();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyName_ShouldFail()
    {
        var cmd = CreateValid() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameExceeds200_ShouldFail()
    {
        var cmd = CreateValid() with { Name = new string('A', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task EmptyCode_ShouldFail()
    {
        var cmd = CreateValid() with { Code = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task CodeExceeds100_ShouldFail()
    {
        var cmd = CreateValid() with { Code = new string('C', 101) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var cmd = CreateValid() with { Email = "invalid" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task NullEmail_ShouldPass()
    {
        var cmd = CreateValid() with { Email = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PhoneExceeds50_ShouldFail()
    {
        var cmd = CreateValid() with { Phone = new string('1', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone");
    }

    [Fact]
    public async Task TaxNumberExceeds50_ShouldFail()
    {
        var cmd = CreateValid() with { TaxNumber = new string('T', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public async Task TaxOfficeExceeds200_ShouldFail()
    {
        var cmd = CreateValid() with { TaxOffice = new string('O', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxOffice");
    }

    private static CreateCustomerCommand CreateValid() => new(
        Name: "Test Müşteri Ltd.",
        Code: "MUS-001",
        Email: "info@test.com",
        Phone: "05551234567"
    );
}
