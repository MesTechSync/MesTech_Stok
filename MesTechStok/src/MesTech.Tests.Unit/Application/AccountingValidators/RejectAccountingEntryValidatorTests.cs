using FluentAssertions;
using MesTech.Application.Commands.RejectAccountingEntry;
using Xunit;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RejectAccountingEntryValidatorTests
{
    private readonly RejectAccountingEntryValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DocumentId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DocumentId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentId");
    }

    [Fact]
    public async Task RejectedBy_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RejectedBy = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RejectedBy");
    }

    [Fact]
    public async Task RejectedBy_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RejectedBy = new string('R', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RejectedBy");
    }

    [Fact]
    public async Task RejectionSource_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RejectionSource = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RejectionSource");
    }

    [Fact]
    public async Task RejectionSource_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { RejectionSource = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RejectionSource");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static RejectAccountingEntryCommand CreateValidCommand() => new()
    {
        DocumentId = Guid.NewGuid(),
        RejectedBy = "auditor@mestech.com",
        RejectionSource = "WebDashboard",
        Reason = "Amount mismatch with bank statement",
        TenantId = Guid.NewGuid()
    };
}
