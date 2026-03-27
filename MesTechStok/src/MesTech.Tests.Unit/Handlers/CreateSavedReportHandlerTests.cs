using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateSavedReportHandlerTests
{
    private readonly Mock<ISavedReportRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateSavedReportHandler _sut;

    public CreateSavedReportHandlerTests()
    {
        _sut = new CreateSavedReportHandler(
            _repoMock.Object, _uowMock.Object, Mock.Of<ILogger<CreateSavedReportHandler>>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndSaves()
    {
        var cmd = new CreateSavedReportCommand(Guid.NewGuid(), "Aylık Satış", "Sales", "{}");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SavedReport>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
