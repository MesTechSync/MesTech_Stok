using FluentAssertions;
using MesTech.Application.Commands.CreateSupplier;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Suppliers;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateSupplierValidatorTests
{
    private readonly CreateSupplierValidator _sut = new();

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
        var cmd = CreateValid() with { Name = new string('S', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
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
        var cmd = CreateValid() with { Code = new string('X', 101) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var cmd = CreateValid() with { Email = "not-valid" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task PhoneExceeds50_ShouldFail()
    {
        var cmd = CreateValid() with { Phone = new string('1', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TaxNumberExceeds50_ShouldFail()
    {
        var cmd = CreateValid() with { TaxNumber = new string('T', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task AllNullOptionals_ShouldPass()
    {
        var cmd = new CreateSupplierCommand(Name: "Tedarikçi", Code: "TDK-001");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateSupplierCommand CreateValid() => new(
        Name: "Test Tedarikçi A.Ş.",
        Code: "TDK-001",
        Email: "info@tedarik.com",
        Phone: "02121234567"
    );
}
