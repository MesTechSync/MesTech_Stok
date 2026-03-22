using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdateCounterpartyHandlerTests
{
    private readonly Mock<ICounterpartyRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateCounterpartyHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdateCounterpartyHandlerTests()
    {
        _sut = new UpdateCounterpartyHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCounterparty_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var counterparty = Counterparty.Create(TenantId, "Old Name", CounterpartyType.Supplier);
        _repoMock.Setup(r => r.GetByIdAsync(counterparty.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(counterparty);
        var command = new UpdateCounterpartyCommand(counterparty.Id, "New Name", Email: "test@test.com");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(counterparty, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCounterparty_ShouldReturnFalse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Counterparty?)null);
        var command = new UpdateCounterpartyCommand(Guid.NewGuid(), "Any Name");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Counterparty>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
