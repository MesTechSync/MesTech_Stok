using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateCounterparty;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateCounterpartyHandlerTests
{
    private readonly Mock<ICounterpartyRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateCounterpartyHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateCounterpartyHandlerTests()
    {
        _sut = new CreateCounterpartyHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateCounterpartyCommand(
            TenantId, "Tedarikci A", CounterpartyType.Supplier,
            VKN: "1234567890", Phone: "05551234567");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Counterparty>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PlatformType_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreateCounterpartyCommand(
            TenantId, "Trendyol", CounterpartyType.Platform, Platform: "Trendyol");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(
            It.Is<Counterparty>(c => c.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
