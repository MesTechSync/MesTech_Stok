using FluentAssertions;
using MesTech.Application.Commands.UpdateSupplier;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Suppliers;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class UpdateSupplierValidatorTests
{
    private readonly UpdateSupplierValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValid();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_ShouldFail()
    {
        var cmd = CreateValid() with { Id = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task EmptyName_ShouldFail()
    {
        var cmd = CreateValid() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyCode_ShouldFail()
    {
        var cmd = CreateValid() with { Code = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidEmail_ShouldFail()
    {
        var cmd = CreateValid() with { Email = "x" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task TaxOfficeExceeds200_ShouldFail()
    {
        var cmd = CreateValid() with { TaxOffice = new string('V', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CityExceeds100_ShouldFail()
    {
        var cmd = CreateValid() with { City = new string('C', 101) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    private static UpdateSupplierCommand CreateValid() => new(
        Id: Guid.NewGuid(),
        Name: "Güncel Tedarikçi",
        Code: "TDK-001"
    );
}
