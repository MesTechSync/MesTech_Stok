using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveImportTemplate;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Settings;

[Trait("Category", "Unit")]
public class SaveImportTemplateValidatorTests
{
    private readonly SaveImportTemplateValidator _sut = new();

    private static SaveImportTemplateCommand CreateValidCommand() =>
        new(
            TenantId: Guid.NewGuid(),
            TemplateName: "Default Import",
            FileFormat: "CSV",
            ColumnMappings: new Dictionary<string, string>
            {
                { "SKU", "product_sku" },
                { "Name", "product_name" }
            });

    [Fact]
    public async Task ValidCommand_ShouldPassValidation()
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
    public async Task EmptyTemplateName_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TemplateName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public async Task TemplateNameExceeds200Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TemplateName = new string('A', 201) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TemplateName");
    }

    [Fact]
    public async Task TemplateNameExactly200Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { TemplateName = new string('A', 200) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFileFormat_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FileFormat = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileFormat");
    }

    [Fact]
    public async Task FileFormatExceeds50Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { FileFormat = new string('F', 51) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileFormat");
    }

    [Fact]
    public async Task FileFormatExactly50Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { FileFormat = new string('F', 50) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullColumnMappings_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ColumnMappings = null! };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ColumnMappings");
    }

    [Fact]
    public async Task EmptyColumnMappings_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ColumnMappings = new Dictionary<string, string>() };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        var cmd = new SaveImportTemplateCommand(
            TenantId: Guid.Empty,
            TemplateName: "",
            FileFormat: "",
            ColumnMappings: null!);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(4);
    }
}
