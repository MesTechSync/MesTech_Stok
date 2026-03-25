using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;
using MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;
using MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for tax handlers — TaxRecord CRUD, RecordTaxWithholding,
/// GetKdvReport, GenerateTaxCalendar.
/// </summary>
[Trait("Category", "Unit")]
public class TaxHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ CreateTaxRecordHandler ═══════

    [Fact]
    public async Task CreateTaxRecord_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateTaxRecordHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateTaxRecord_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateTaxRecordHandler(repo.Object, uow.Object);
        var cmd = new CreateTaxRecordCommand(
            _tenantId, "2026-03", "KDV", 10_000m, 0.20m, 2_000m,
            new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc));

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<TaxRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ DeleteTaxRecordHandler ═══════

    [Fact]
    public async Task DeleteTaxRecord_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRecord?)null);

        var sut = new DeleteTaxRecordHandler(repo.Object, uow.Object);
        var cmd = new DeleteTaxRecordCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ UpdateTaxRecordHandler ═══════

    [Fact]
    public async Task UpdateTaxRecord_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ITaxRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRecord?)null);

        var sut = new UpdateTaxRecordHandler(repo.Object, uow.Object);
        var cmd = new UpdateTaxRecordCommand(Guid.NewGuid(), MarkAsPaid: true);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ RecordTaxWithholdingHandler ═══════

    [Fact]
    public async Task RecordTaxWithholding_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new RecordTaxWithholdingHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RecordTaxWithholding_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<ITaxWithholdingRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new RecordTaxWithholdingHandler(repo.Object, uow.Object);
        var cmd = new RecordTaxWithholdingCommand(_tenantId, 1000m, 0.20m, "KDV");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<TaxWithholding>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ GetKdvReportHandler ═══════

    [Fact]
    public async Task GetKdvReport_NullRequest_ThrowsArgumentNullException()
    {
        var mediator = new Mock<ISender>();
        var sut = new GetKdvReportHandler(mediator.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GenerateTaxCalendarHandler ═══════

    [Fact]
    public async Task GenerateTaxCalendar_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new GenerateTaxCalendarHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateTaxCalendar_ValidYear_ReturnsPositiveCount()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new GenerateTaxCalendarHandler(repo.Object, uow.Object);
        var cmd = new GenerateTaxCalendarCommand(2026, _tenantId);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeGreaterThan(0, "tax calendar should generate ~40 events per year");
    }
}
