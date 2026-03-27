using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeleteSavedReportHandlerTests
{
    private readonly Mock<ISavedReportRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteSavedReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DeleteSavedReportHandlerTests()
    {
        _sut = new DeleteSavedReportHandler(
            _repoMock.Object,
            _uowMock.Object,
            Mock.Of<ILogger<DeleteSavedReportHandler>>());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NonExistentReport_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Reporting.SavedReport?)null);

        var cmd = new DeleteSavedReportCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
