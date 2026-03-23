using FluentAssertions;
using MesTech.Application.Commands.ApproveAccountingEntry;
using Xunit;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ApproveAccountingEntryValidatorTests
{
    private readonly ApproveAccountingEntryValidator _sut = new();

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
    public async Task ApprovedBy_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ApprovedBy = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovedBy");
    }

    [Fact]
    public async Task ApprovedBy_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ApprovedBy = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovedBy");
    }

    [Fact]
    public async Task ApprovalSource_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ApprovalSource = string.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovalSource");
    }

    [Fact]
    public async Task ApprovalSource_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ApprovalSource = new string('S', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ApprovalSource");
    }

    [Fact]
    public async Task TenantId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    private static ApproveAccountingEntryCommand CreateValidCommand() => new()
    {
        DocumentId = Guid.NewGuid(),
        ApprovedBy = "admin@mestech.com",
        ApprovalSource = "WebDashboard",
        TenantId = Guid.NewGuid()
    };
}
