using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// DeleteChartOfAccountHandler tests — soft-delete and not-found / system-guard scenarios.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class DeleteChartOfAccountHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly DeleteChartOfAccountHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteChartOfAccountHandlerTests()
    {
        _repoMock = new Mock<IChartOfAccountsRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _sut = new DeleteChartOfAccountHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTrueAndCallsUpdateAsync()
    {
        // Arrange
        var account = ChartOfAccounts.Create(_tenantId, "320", "Saticilar", AccountType.Liability);
        var command = new DeleteChartOfAccountCommand(account.Id, "admin");

        _repoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _repoMock.Verify(
            r => r.UpdateAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_AccountNotFound_ReturnsFalse()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var command = new DeleteChartOfAccountCommand(missingId, "admin");

        _repoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        _repoMock.Verify(
            r => r.UpdateAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()),
            Times.Never());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Handle_SystemAccount_ThrowsInvalidOperationException()
    {
        // Arrange
        var account = ChartOfAccounts.Create(_tenantId, "100", "Kasa", AccountType.Asset);
        account.MarkAsSystem();

        var command = new DeleteChartOfAccountCommand(account.Id, "admin");

        _repoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*System account*");

        _repoMock.Verify(
            r => r.UpdateAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}
