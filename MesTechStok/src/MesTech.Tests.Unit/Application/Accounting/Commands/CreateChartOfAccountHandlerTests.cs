using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateChartOfAccountHandlerTests
{
    private readonly Mock<IChartOfAccountsRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateChartOfAccountHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateChartOfAccountHandlerTests()
    {
        _sut = new CreateChartOfAccountHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnNewId()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByCodeAsync(TenantId, "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChartOfAccounts?)null);
        var command = new CreateChartOfAccountCommand(TenantId, "100", "Kasa", AccountType.Asset);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<ChartOfAccounts>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var existing = ChartOfAccounts.Create(TenantId, "100", "Kasa", AccountType.Asset);
        _repoMock.Setup(r => r.GetByCodeAsync(TenantId, "100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var command = new CreateChartOfAccountCommand(TenantId, "100", "Kasa Duplicate", AccountType.Asset);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
