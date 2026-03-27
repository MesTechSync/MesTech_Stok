using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Events;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Events;
using MesTech.Domain.Events.Crm;
using MesTech.Domain.Events.Calendar;
using MesTech.Domain.Events.Documents;
using MesTech.Domain.Events.Finance;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for logging-only event handlers — verifies HandleAsync completes without exception.
/// </summary>
[Trait("Category", "Unit")]
public class EventHandlerLoggingTests
{
    [Fact]
    public async Task BaBsRecordCreatedEventHandler_CompletesSuccessfully()
    {
        var sut = new BaBsRecordCreatedEventHandler(NullLogger<BaBsRecordCreatedEventHandler>.Instance);
        var evt = new BaBsRecordCreatedEvent
        {
            BaBsRecordId = Guid.NewGuid(),
            Type = BaBsType.Ba,
            Year = 2026, Month = 3,
            CounterpartyVkn = "1234567890",
            TotalAmount = 5000m
        };

        var act = async () => await sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BankStatementImportedEventHandler_CompletesSuccessfully()
    {
        var sut = new BankStatementImportedEventHandler(NullLogger<BankStatementImportedEventHandler>.Instance);
        var evt = new BankStatementImportedEvent
        {
            BankAccountId = Guid.NewGuid(),
            TransactionCount = 15,
            TotalInflow = 10000m,
            TotalOutflow = 3000m
        };

        var act = async () => await sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EInvoiceCreatedEventHandler_CompletesSuccessfully()
    {
        var sut = new EInvoiceCreatedEventHandler(NullLogger<EInvoiceCreatedEventHandler>.Instance);

        var act = async () => await sut.HandleAsync(Guid.NewGuid(), "ETTN-001", MesTech.Domain.Enums.EInvoiceType.SATIS, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EInvoiceCancelledEventHandler_CompletesSuccessfully()
    {
        var sut = new EInvoiceCancelledEventHandler(NullLogger<EInvoiceCancelledEventHandler>.Instance);

        var act = async () => await sut.HandleAsync(Guid.NewGuid(), "ETTN-002", "Musteri iade", CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EInvoiceSentEventHandler_CompletesSuccessfully()
    {
        var sut = new EInvoiceSentEventHandler(NullLogger<EInvoiceSentEventHandler>.Instance);

        var act = () => sut.HandleAsync(Guid.NewGuid(), "ETTN-003", "REF-001", CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExpenseCreatedEventHandler_CompletesSuccessfully()
    {
        var sut = new ExpenseCreatedEventHandler(NullLogger<ExpenseCreatedEventHandler>.Instance);
        var evt = new ExpenseCreatedEvent
        {
            ExpenseId = Guid.NewGuid(),
            Title = "Kargo masrafi",
            Amount = 100m,
            Source = ExpenseSource.Manual
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExpensePaidEventHandler_CompletesSuccessfully()
    {
        var sut = new ExpensePaidEventHandler(NullLogger<ExpensePaidEventHandler>.Instance);
        var evt = new ExpensePaidEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExpenseApprovedEventHandler_CompletesSuccessfully()
    {
        var sut = new ExpenseApprovedEventHandler(NullLogger<ExpenseApprovedEventHandler>.Instance);

        var act = () => sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DealWonEventHandler_CompletesSuccessfully()
    {
        var sut = new DealWonEventHandler(NullLogger<DealWonEventHandler>.Instance);
        var evt = new DealWonEvent(Guid.NewGuid(), Guid.NewGuid(), null, 5000m, DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DealLostEventHandler_CompletesSuccessfully()
    {
        var sut = new DealLostEventHandler(NullLogger<DealLostEventHandler>.Instance);
        var evt = new DealLostEvent(Guid.NewGuid(), Guid.NewGuid(), "Fiyat uyumsuzlugu", DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DealStageChangedEventHandler_CompletesSuccessfully()
    {
        var sut = new DealStageChangedEventHandler(NullLogger<DealStageChangedEventHandler>.Instance);
        var evt = new DealStageChangedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CalendarEventCreatedEventHandler_CompletesSuccessfully()
    {
        var sut = new CalendarEventCreatedEventHandler(NullLogger<CalendarEventCreatedEventHandler>.Instance);
        var evt = new CalendarEventCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CashTransactionRecordedEventHandler_CompletesSuccessfully()
    {
        var sut = new CashTransactionRecordedEventHandler(NullLogger<CashTransactionRecordedEventHandler>.Instance);
        var evt = new CashTransactionRecordedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CashTransactionType.Income, 250m, 1250m, DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DocumentUploadedEventHandler_CompletesSuccessfully()
    {
        var sut = new DocumentUploadedEventHandler(NullLogger<DocumentUploadedEventHandler>.Instance);
        var evt = new DocumentUploadedEvent(Guid.NewGuid(), "fatura.pdf", 1024L, Guid.NewGuid(), DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DailySummaryGeneratedEventHandler_CompletesSuccessfully()
    {
        var sut = new DailySummaryGeneratedEventHandler(NullLogger<DailySummaryGeneratedEventHandler>.Instance);
        var evt = new DailySummaryGeneratedEvent(
            Guid.NewGuid(), DateTime.UtcNow.Date, 42, 15000m, 3, 10, DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FixedAssetCreatedEventHandler_CompletesSuccessfully()
    {
        var sut = new FixedAssetCreatedEventHandler(NullLogger<FixedAssetCreatedEventHandler>.Instance);
        var evt = new FixedAssetCreatedEvent
        {
            FixedAssetId = Guid.NewGuid(),
            AssetName = "Bilgisayar",
            AssetCode = "FA-001",
            AcquisitionCost = 25000m,
            Method = DepreciationMethod.StraightLine
        };

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BuyboxLostEventHandler_CompletesSuccessfully()
    {
        var sut = new BuyboxLostEventHandler(NullLogger<BuyboxLostEventHandler>.Instance);
        var evt = new BuyboxLostEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-001",
            100m, 90m, "Rakip A", DateTime.UtcNow);

        var act = () => sut.HandleAsync(evt, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
