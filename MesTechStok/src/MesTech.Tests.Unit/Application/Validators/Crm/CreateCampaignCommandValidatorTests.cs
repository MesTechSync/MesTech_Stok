using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateCampaignCommandValidatorTests
{
    private readonly CreateCampaignCommandValidator _sut = new();

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
    public async Task EmptyName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameExceeds200Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameExactly200Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Name = new string('A', 200) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task StartDateAfterEndDate_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task StartDateEqualsEndDate_ShouldFail()
    {
        var date = DateTime.UtcNow.AddDays(5);
        var cmd = CreateValidCommand() with { StartDate = date, EndDate = date };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StartDate");
    }

    [Fact]
    public async Task DiscountPercentNegative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DiscountPercent = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DiscountPercent");
    }

    [Fact]
    public async Task DiscountPercentOver100_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DiscountPercent = 101 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DiscountPercent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task DiscountPercentInRange_ShouldPass(decimal discount)
    {
        var cmd = CreateValidCommand() with { DiscountPercent = discount };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateCampaignCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Yaz Kampanyası",
        StartDate: DateTime.UtcNow.AddDays(1),
        EndDate: DateTime.UtcNow.AddDays(30),
        DiscountPercent: 15
    );
}
