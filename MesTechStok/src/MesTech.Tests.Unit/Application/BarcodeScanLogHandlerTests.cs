using FluentAssertions;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// Task 9: BarcodeScanLog CQRS Handler Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class GetBarcodeScanLogsHandlerTests
{
    private readonly Mock<IBarcodeScanLogRepository> _repo = new();

    private GetBarcodeScanLogsHandler CreateHandler() => new(_repo.Object);

    [Fact]
    public async Task Handle_ReturnsPagedResults()
    {
        // Arrange
        var logs = new List<BarcodeScanLog>
        {
            new() { Barcode = "8680000111111", Format = "EAN13", Source = "Camera", IsValid = true, RawLength = 13 },
            new() { Barcode = "8680000222222", Format = "EAN13", Source = "Scanner", IsValid = true, RawLength = 13 }
        };
        _repo.Setup(r => r.GetPagedAsync(1, 50, null, null, null, null, null))
            .ReturnsAsync(logs.AsReadOnly());
        _repo.Setup(r => r.GetCountAsync(null, null, null, null, null))
            .ReturnsAsync(2);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(new GetBarcodeScanLogsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var logs = new List<BarcodeScanLog>
        {
            new() { Barcode = "8680000111111", Format = "EAN13", Source = "Camera", IsValid = true, RawLength = 13 }
        };
        _repo.Setup(r => r.GetPagedAsync(2, 10, "8680", "Camera", true, from, to))
            .ReturnsAsync(logs.AsReadOnly());
        _repo.Setup(r => r.GetCountAsync("8680", "Camera", true, from, to))
            .ReturnsAsync(1);

        // Act
        var handler = CreateHandler();
        var query = new GetBarcodeScanLogsQuery(
            Page: 2, PageSize: 10,
            BarcodeFilter: "8680", SourceFilter: "Camera",
            IsValidFilter: true, From: from, To: to);
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        _repo.Verify(r => r.GetPagedAsync(2, 10, "8680", "Camera", true, from, to), Times.Once);
        _repo.Verify(r => r.GetCountAsync("8680", "Camera", true, from, to), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        _repo.Setup(r => r.GetPagedAsync(1, 50, null, null, null, null, null))
            .ReturnsAsync(new List<BarcodeScanLog>().AsReadOnly());
        _repo.Setup(r => r.GetCountAsync(null, null, null, null, null))
            .ReturnsAsync(0);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(new GetBarcodeScanLogsQuery(), CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}

[Trait("Category", "Unit")]
public class CreateBarcodeScanLogHandlerTests
{
    private readonly Mock<IBarcodeScanLogRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateBarcodeScanLogHandler CreateHandler() => new(_repo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidInput_CreatesLogAndReturnsSuccess()
    {
        // Arrange
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateBarcodeScanLogCommand(
            Barcode: "8680000111111",
            Format: "EAN13",
            Source: "Camera",
            DeviceId: "device-01",
            IsValid: true,
            ValidationMessage: null,
            RawLength: 13,
            CorrelationId: "corr-001");

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.LogId.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.Is<BarcodeScanLog>(log =>
            log.Barcode == "8680000111111" &&
            log.Format == "EAN13" &&
            log.Source == "Camera" &&
            log.DeviceId == "device-01" &&
            log.IsValid == true &&
            log.RawLength == 13 &&
            log.CorrelationId == "corr-001"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsUnitOfWorkSaveChanges()
    {
        // Arrange
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateBarcodeScanLogCommand(
            Barcode: "8680000222222",
            Format: "Code128",
            Source: "Scanner");

        // Act
        var handler = CreateHandler();
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
