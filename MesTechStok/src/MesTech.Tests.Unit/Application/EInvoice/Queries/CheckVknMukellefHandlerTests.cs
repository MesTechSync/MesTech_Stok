using FluentAssertions;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.EInvoice.Queries;

[Trait("Category", "Unit")]
public class CheckVknMukellefHandlerTests
{
    private readonly Mock<IEInvoiceProvider> _provider = new();

    private CheckVknMukellefHandler CreateHandler() =>
        new(_provider.Object);

    [Fact]
    public async Task Handle_ValidVkn_ShouldReturnMukellefResult()
    {
        // Arrange
        var vkn = "1234567890";
        var expected = new VknMukellefResult(
            vkn, IsEInvoiceMukellef: true, IsEArchiveMukellef: true,
            Title: "MesTech A.S.", CheckedAt: DateTime.UtcNow);

        _provider
            .Setup(p => p.CheckVknMukellefAsync(vkn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var query = new CheckVknMukellefQuery(vkn);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Vkn.Should().Be(vkn);
        result.IsEInvoiceMukellef.Should().BeTrue();
        result.Title.Should().Be("MesTech A.S.");
    }

    [Fact]
    public async Task Handle_NonMukellefVkn_ShouldReturnFalseFlags()
    {
        // Arrange
        var vkn = "9999999999";
        var expected = new VknMukellefResult(
            vkn, IsEInvoiceMukellef: false, IsEArchiveMukellef: false,
            Title: null, CheckedAt: DateTime.UtcNow);

        _provider
            .Setup(p => p.CheckVknMukellefAsync(vkn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = CreateHandler();
        var query = new CheckVknMukellefQuery(vkn);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsEInvoiceMukellef.Should().BeFalse();
        result.IsEArchiveMukellef.Should().BeFalse();
        result.Title.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
