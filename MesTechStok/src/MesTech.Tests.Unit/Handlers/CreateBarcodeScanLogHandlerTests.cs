using FluentAssertions;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateBarcodeScanLogHandlerTests
{
    private readonly Mock<IBarcodeScanLogRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateBarcodeScanLogHandler _sut;

    public CreateBarcodeScanLogHandlerTests()
    {
        _sut = new CreateBarcodeScanLogHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesLogAndReturnsSuccess()
    {
        var cmd = new CreateBarcodeScanLogCommand("8680001234567", "EAN13", "SCANNER");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.LogId.Should().NotBe(Guid.Empty);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        var act = () => new CreateBarcodeScanLogHandler(null!, _uowMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }
}
