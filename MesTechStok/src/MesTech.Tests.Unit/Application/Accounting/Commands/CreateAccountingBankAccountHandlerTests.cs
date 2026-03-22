using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateAccountingBankAccount;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateAccountingBankAccountHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateAccountingBankAccountHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateAccountingBankAccountHandlerTests()
    {
        _sut = new CreateAccountingBankAccountHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateAccountingBankAccountCommand(
            TenantId, "Isbank Ticari", "TRY", "Isbank",
            "TR330006100519786457841326", "1234567890");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultAccount_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreateAccountingBankAccountCommand(
            TenantId, "Garanti USD", "USD", IsDefault: true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
