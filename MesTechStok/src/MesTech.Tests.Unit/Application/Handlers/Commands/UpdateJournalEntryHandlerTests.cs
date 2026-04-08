using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.UpdateJournalEntry;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateJournalEntryHandler testi — yevmiye güncelleme.
/// P1: Muhasebe kaydı bütünlüğü, optimistic concurrency.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class UpdateJournalEntryHandlerTests
{
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateJournalEntryHandler CreateSut() => new(_journalRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_EntryNotFound_ShouldReturnFailure()
    {
        _journalRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntry?)null);

        var cmd = new UpdateJournalEntryCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Test", null, new(), null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TenantMismatch_ShouldReturnFailure()
    {
        var entry = JournalEntry.Create(Guid.NewGuid(), DateTime.UtcNow, "Test");
        _journalRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var differentTenant = Guid.NewGuid();
        var cmd = new UpdateJournalEntryCommand(
            entry.Id, differentTenant, DateTime.UtcNow, "Updated", null, new(), null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Tenant mismatch");
    }

    [Fact]
    public async Task Handle_PostedEntry_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        var entry = JournalEntry.Create(tenantId, DateTime.UtcNow, "Test");
        entry.AddLine(Guid.NewGuid(), 1000m, 0m, "Debit");
        entry.AddLine(Guid.NewGuid(), 0m, 1000m, "Credit");
        entry.Post(); // mark as posted — requires at least 2 balanced lines
        _journalRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var cmd = new UpdateJournalEntryCommand(
            entry.Id, tenantId, DateTime.UtcNow, "Updated", null, new(), null);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("posted");
    }
}
