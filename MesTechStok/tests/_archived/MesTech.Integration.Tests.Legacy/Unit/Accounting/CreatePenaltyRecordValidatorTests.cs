using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Domain.Accounting.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreatePenaltyRecordValidatorTests
{
    private readonly CreatePenaltyRecordValidator _validator = new();

    private static CreatePenaltyRecordCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Source: PenaltySource.Trendyol,
        Description: "Geç kargo cezası",
        Amount: 250m,
        PenaltyDate: DateTime.UtcNow);

    [Fact]
    public void Valid_Command_Passes()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Description_Fails()
    {
        var cmd = ValidCommand() with { Description = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Description_Over500_Fails()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Zero_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = 0m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_Amount_Fails()
    {
        var cmd = ValidCommand() with { Amount = -10m };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Currency_Over3_Fails()
    {
        var cmd = ValidCommand() with { Currency = "ABCD" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_Currency_Fails()
    {
        var cmd = ValidCommand() with { Currency = "" };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReferenceNumber_Over100_Fails()
    {
        var cmd = ValidCommand() with { ReferenceNumber = new string('R', 101) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Notes_Over500_Fails()
    {
        var cmd = ValidCommand() with { Notes = new string('N', 501) };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DueDate_Before_PenaltyDate_Fails()
    {
        var cmd = ValidCommand() with
        {
            PenaltyDate = new DateTime(2026, 6, 1),
            DueDate = new DateTime(2026, 1, 1)
        };
        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DueDate_After_PenaltyDate_Passes()
    {
        var cmd = ValidCommand() with
        {
            PenaltyDate = new DateTime(2026, 1, 1),
            DueDate = new DateTime(2026, 2, 1)
        };
        _validator.Validate(cmd).IsValid.Should().BeTrue();
    }
}
