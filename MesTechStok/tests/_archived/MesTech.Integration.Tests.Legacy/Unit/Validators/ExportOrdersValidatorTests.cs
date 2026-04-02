using FluentAssertions;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ExportOrdersValidatorTests
{
    private readonly ExportOrdersValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ExportOrdersCommand(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new ExportOrdersCommand(
            Guid.Empty,
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void From_After_To_Fails()
    {
        var cmd = new ExportOrdersCommand(
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-7));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "From");
    }
}
