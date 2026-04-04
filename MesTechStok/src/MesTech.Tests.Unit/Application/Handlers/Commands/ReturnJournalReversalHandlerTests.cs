using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ReturnJournalReversalHandlerCommandTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<ReturnJournalReversalHandler>> _logger = new();

    private ReturnJournalReversalHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ZeroRefundAmount_ShouldSkip()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeRefundAmount_ShouldSkip()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -100m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReference_ShouldSkipIdempotently()
    {
        var tenantId = Guid.NewGuid();
        var returnId = Guid.NewGuid();
        var refNumber = $"RET-{returnId.ToString()[..8]}";

        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, refNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.HandleAsync(returnId, Guid.NewGuid(), tenantId, 200m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidReturn_ShouldCreateReversalJournalEntry()
    {
        var tenantId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), tenantId, 300m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidReturn_ShouldCreateTwoLines_DebitAndCredit()
    {
        var tenantId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), tenantId, 500m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Lines.Should().HaveCount(2);
        captured.IsPosted.Should().BeTrue();
    }
}
