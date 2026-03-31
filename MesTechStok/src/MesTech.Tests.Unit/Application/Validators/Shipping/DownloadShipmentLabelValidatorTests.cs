using FluentAssertions;
using MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Shipping;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class DownloadShipmentLabelValidatorTests
{
    private readonly DownloadShipmentLabelValidator _sut = new();

    [Fact]
    public async Task ValidQuery_ShouldPass()
    {
        var query = CreateValidQuery();
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_Empty_ShouldFail()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task ShipmentId_Empty_ShouldFail()
    {
        var query = CreateValidQuery() with { ShipmentId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentId");
    }

    [Fact]
    public async Task Format_Empty_ShouldFail()
    {
        var query = CreateValidQuery() with { Format = "" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Fact]
    public async Task Format_Invalid_ShouldFail()
    {
        var query = CreateValidQuery() with { Format = "DOCX" };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Format");
    }

    [Theory]
    [InlineData("PDF")]
    [InlineData("ZPL")]
    [InlineData("PNG")]
    [InlineData("pdf")]
    [InlineData("zpl")]
    public async Task AllowedFormats_ShouldPass(string format)
    {
        var query = CreateValidQuery() with { Format = format };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NullTrackingNumber_ShouldPass()
    {
        var query = CreateValidQuery() with { TrackingNumber = null };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BothIds_Empty_ShouldFail_WithMultipleErrors()
    {
        var query = CreateValidQuery() with { TenantId = Guid.Empty, ShipmentId = Guid.Empty };
        var result = await _sut.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }

    private static DownloadShipmentLabelQuery CreateValidQuery() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "TR123456789", "PDF");
}
