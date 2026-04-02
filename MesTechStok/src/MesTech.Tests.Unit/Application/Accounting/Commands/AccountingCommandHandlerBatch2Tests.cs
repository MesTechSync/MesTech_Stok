using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.ImportSettlement;
using MesTech.Application.Features.Accounting.Commands.ParseAndImportSettlement;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

/// <summary>
/// Accounting command handler testleri Batch 2 — G28 kapsamı devam.
/// ImportSettlement, ParseAndImportSettlement, UpdateJournalEntry, UpdatePlatformCommissionRate.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>

#region ImportSettlement

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ImportSettlementHandlerTests
{
    private readonly Mock<ISettlementBatchRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ImportSettlementHandler CreateSut() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateBatchAndSave()
    {
        var sut = CreateSut();
        var result = await sut.Handle(
            new ImportSettlementCommand(
                Guid.NewGuid(), "Trendyol",
                DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
                10000m, 1500m, 8500m, []),
            CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ParseAndImportSettlement

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ParseAndImportSettlementHandlerTests
{
    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        var parserFactory = new Mock<ISettlementParserFactory>();
        var repo = new Mock<ISettlementBatchRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<ParseAndImportSettlementHandler>>();

        var sut = new ParseAndImportSettlementHandler(parserFactory.Object, repo.Object, uow.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

#endregion

#region UpdateJournalEntry

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class UpdateJournalEntryHandlerTests2
{
    private readonly Mock<IJournalEntryRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateJournalEntryHandler CreateSut() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_EntryNotFound_ShouldReturnError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntry?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new UpdateJournalEntryCommand(
                Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
                "Test", null, [], null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region UpdatePlatformCommissionRate

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class UpdatePlatformCommissionRateHandlerTests2
{
    private readonly Mock<IPlatformCommissionRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdatePlatformCommissionRateHandler CreateSut() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_NotFound_ShouldReturnFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformCommission?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new UpdatePlatformCommissionRateCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion
