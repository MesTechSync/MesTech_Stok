using FluentAssertions;
using MesTech.Application.Features.Reporting.Commands.CreateSavedReport;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class ReportingHandlerTests
{
    private readonly Mock<ISavedReportRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task CreateSavedReport_ValidCommand_ReturnsNonEmptyGuid()
    {
        var logger = new Mock<ILogger<CreateSavedReportHandler>>();
        _repo.Setup(r => r.AddAsync(It.IsAny<SavedReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CreateSavedReportCommand(
            TenantId: Guid.NewGuid(), Name: "Monthly Sales", ReportType: "SalesReport",
            FilterJson: "{\"month\":3}", CreatedByUserId: Guid.NewGuid());
        var handler = new CreateSavedReportHandler(_repo.Object, _uow.Object, logger.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task DeleteSavedReport_NotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedReport?)null);
        var logger = new Mock<ILogger<DeleteSavedReportHandler>>();
        var cmd = new DeleteSavedReportCommand(TenantId: Guid.NewGuid(), ReportId: Guid.NewGuid());
        var handler = new DeleteSavedReportHandler(_repo.Object, _uow.Object, logger.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.Should().BeFalse();
    }
}
