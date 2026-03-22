using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdateChartOfAccountHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateChartOfAccountHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdateChartOfAccountHandlerTests()
    {
        _sut = new UpdateChartOfAccountHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingAccount_ShouldReturnTrue()
    {
        // Arrange
        var account = ChartOfAccounts.Create(TenantId, "100", "Kasa", AccountType.Asset);
        _repoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        var command = new UpdateChartOfAccountCommand(account.Id, "Kasa Hesabi");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentAccount_ShouldReturnFalse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);
        var command = new UpdateChartOfAccountCommand(Guid.NewGuid(), "New Name");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
