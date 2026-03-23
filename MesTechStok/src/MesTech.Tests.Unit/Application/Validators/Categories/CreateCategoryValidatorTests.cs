using FluentAssertions;
using MesTech.Application.Commands.CreateCategory;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Categories;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Name_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenNull_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = null! };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Name = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Name_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Name = new string('N', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Code_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Code = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenNull_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Code = null! };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Code = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public async Task Code_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Code = new string('C', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateCategoryCommand CreateValidCommand() => new(
        Name: "Elektronik",
        Code: "ELK-001"
    );
}
