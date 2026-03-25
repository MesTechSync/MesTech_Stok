using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;
using MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;
using MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for salary, penalty and fixed expense handlers — CRUD operations
/// for SalaryRecord, PenaltyRecord, FixedExpense.
/// </summary>
[Trait("Category", "Unit")]
public class SalaryPenaltyHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ CreateSalaryRecordHandler ═══════

    [Fact]
    public async Task CreateSalaryRecord_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateSalaryRecordHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateSalaryRecord_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateSalaryRecordHandler(repo.Object, uow.Object);
        var cmd = new CreateSalaryRecordCommand(
            _tenantId, "Ali Yilmaz", 25_000m, 3_750m, 3_500m, 3_325m, 189.75m, 2026, 3);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<SalaryRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ DeleteSalaryRecordHandler ═══════

    [Fact]
    public async Task DeleteSalaryRecord_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalaryRecord?)null);

        var sut = new DeleteSalaryRecordHandler(repo.Object, uow.Object);
        var cmd = new DeleteSalaryRecordCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ UpdateSalaryRecordHandler ═══════

    [Fact]
    public async Task UpdateSalaryRecord_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalaryRecord?)null);

        var sut = new UpdateSalaryRecordHandler(repo.Object, uow.Object);
        var cmd = new UpdateSalaryRecordCommand(Guid.NewGuid(), PaymentStatus.Completed);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ CreatePenaltyRecordHandler ═══════

    [Fact]
    public async Task CreatePenaltyRecord_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreatePenaltyRecordHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePenaltyRecord_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreatePenaltyRecordHandler(repo.Object, uow.Object);
        var cmd = new CreatePenaltyRecordCommand(
            _tenantId, PenaltySource.Trendyol, "Gec teslimat cezasi", 150m,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc));

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<PenaltyRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ DeletePenaltyRecordHandler ═══════

    [Fact]
    public async Task DeletePenaltyRecord_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PenaltyRecord?)null);

        var sut = new DeletePenaltyRecordHandler(repo.Object, uow.Object);
        var cmd = new DeletePenaltyRecordCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ UpdatePenaltyRecordHandler ═══════

    [Fact]
    public async Task UpdatePenaltyRecord_NotFound_ThrowsKeyNotFoundException()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        var uow = new Mock<IUnitOfWork>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PenaltyRecord?)null);

        var sut = new UpdatePenaltyRecordHandler(repo.Object, uow.Object);
        var cmd = new UpdatePenaltyRecordCommand(Guid.NewGuid(), PaymentStatus.Completed);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.Handle(cmd, CancellationToken.None));
    }

    // ═══════ CreateFixedExpenseHandler ═══════

    [Fact]
    public async Task CreateFixedExpense_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateFixedExpenseHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFixedExpense_ValidRequest_ReturnsGuid()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CreateFixedExpenseHandler(repo.Object, uow.Object);
        var cmd = new CreateFixedExpenseCommand(
            _tenantId, "Ofis Kirasi", 12_000m, 1,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<FixedExpense>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
