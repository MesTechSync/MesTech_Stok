using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateBaBsRecordHandlerTests
{
    private readonly Mock<IBaBsRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateBaBsRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateBaBsRecordHandlerTests()
    {
        _sut = new CreateBaBsRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidBaRecord_CreatesAndReturnsId()
    {
        // Arrange
        var command = new CreateBaBsRecordCommand(
            TenantId, 2026, 3, BaBsType.Ba,
            "1234567890", "Tedarikci A.S.", 15_000m, 5);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(
            It.IsAny<MesTech.Domain.Accounting.Entities.BaBsRecord>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_BsTypeRecord_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateBaBsRecordCommand(
            TenantId, 2026, 2, BaBsType.Bs,
            "9876543210", "Musteri Ltd.", 50_000m, 12);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
