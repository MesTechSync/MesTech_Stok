using FluentAssertions;
using MesTech.Application.Features.Tasks.Commands.CreateProject;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Tasks;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "Yeni Proje");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = new CreateProjectCommand(Guid.Empty, "Yeni Proje");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyName_ShouldFail()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameExceeds500Chars_ShouldFail()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), new string('N', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task DescriptionExceeds500Chars_ShouldFail()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "Proje", Description: new string('D', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task DescriptionNull_ShouldPass()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "Proje", Description: null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ColorExceeds500Chars_ShouldFail()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "Proje", Color: new string('#', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Color");
    }

    [Fact]
    public async Task ColorNull_ShouldPass()
    {
        var cmd = new CreateProjectCommand(Guid.NewGuid(), "Proje", Color: null);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
