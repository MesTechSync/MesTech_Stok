using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tasks;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateWorkTaskValidatorTests
{
    private readonly CreateWorkTaskValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CreateWorkTaskCommand(Guid.NewGuid(), "Gorev basligi");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new CreateWorkTaskCommand(Guid.Empty, "Gorev");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyTitle_ShouldFail()
    {
        var cmd = new CreateWorkTaskCommand(Guid.NewGuid(), "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task TitleExceeds500Chars_ShouldFail()
    {
        var cmd = new CreateWorkTaskCommand(Guid.NewGuid(), new string('T', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }
}
