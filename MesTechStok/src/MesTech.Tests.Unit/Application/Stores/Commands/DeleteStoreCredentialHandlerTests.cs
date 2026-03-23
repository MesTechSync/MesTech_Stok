using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Stores.Commands;

[Trait("Category", "Unit")]
public class DeleteStoreCredentialHandlerTests
{
    private readonly Mock<IStoreCredentialRepository> _credRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<DeleteStoreCredentialHandler>> _loggerMock = new();
    private readonly DeleteStoreCredentialHandler _sut;
    private static readonly Guid StoreId = Guid.NewGuid();

    public DeleteStoreCredentialHandlerTests()
    {
        _sut = new DeleteStoreCredentialHandler(_credRepoMock.Object, _uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CredentialsExist_ShouldSoftDeleteAndReturnTrue()
    {
        // Arrange
        var cred1 = new StoreCredential
        {
            TenantId = Guid.NewGuid(), StoreId = StoreId,
            Key = "api_key:ApiKey", EncryptedValue = "enc1"
        };
        var cred2 = new StoreCredential
        {
            TenantId = Guid.NewGuid(), StoreId = StoreId,
            Key = "api_key:Secret", EncryptedValue = "enc2"
        };
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { cred1, cred2 }.AsReadOnly());

        // Act
        var result = await _sut.Handle(
            new DeleteStoreCredentialCommand(StoreId, "admin"), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        cred1.IsDeleted.Should().BeTrue();
        cred2.IsDeleted.Should().BeTrue();
        cred1.DeletedBy.Should().Be("admin");
        cred2.DeletedBy.Should().Be("admin");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCredentials_ShouldReturnFalse()
    {
        // Arrange
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential>().AsReadOnly());

        // Act
        var result = await _sut.Handle(
            new DeleteStoreCredentialCommand(StoreId), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_DefaultDeletedBy_ShouldUseSystem()
    {
        // Arrange
        var cred = new StoreCredential
        {
            TenantId = Guid.NewGuid(), StoreId = StoreId,
            Key = "api_key:Token", EncryptedValue = "enc"
        };
        _credRepoMock.Setup(r => r.GetByStoreIdAsync(StoreId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StoreCredential> { cred }.AsReadOnly());

        // Act — default DeletedBy is "system"
        await _sut.Handle(new DeleteStoreCredentialCommand(StoreId), CancellationToken.None);

        // Assert
        cred.DeletedBy.Should().Be("system");
    }
}
