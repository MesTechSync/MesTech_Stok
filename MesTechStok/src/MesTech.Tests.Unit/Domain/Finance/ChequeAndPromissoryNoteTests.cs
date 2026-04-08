using FluentAssertions;
using MesTech.Domain.Entities.Finance;

namespace MesTech.Tests.Unit.Domain.Finance;

/// <summary>
/// S1-DEV5-02: Cheque + PromissoryNote entity testleri.
/// Create, IsOverdue, Status geçişleri, karşılıksız, ciro.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Finance")]
public class ChequeAndPromissoryNoteTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // ══════════════════════════════════════
    // Cheque
    // ══════════════════════════════════════

    [Fact]
    public void Cheque_Create_Valid_ShouldSucceed()
    {
        var c = Cheque.Create(TenantId, "CHK-001", 5000m,
            new DateTime(2026, 3, 1), new DateTime(2026, 6, 1),
            "Garanti BBVA", ChequeType.Received, "Ahmet Yilmaz");

        c.ChequeNumber.Should().Be("CHK-001");
        c.Amount.Should().Be(5000m);
        c.BankName.Should().Be("Garanti BBVA");
        c.Type.Should().Be(ChequeType.Received);
        c.Status.Should().Be(ChequeStatus.InPortfolio);
        c.DrawerName.Should().Be("Ahmet Yilmaz");
    }

    [Fact]
    public void Cheque_Create_ZeroAmount_ShouldThrow()
    {
        var act = () => Cheque.Create(TenantId, "CHK-BAD", 0m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(3), "Banka", ChequeType.Given);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Cheque_Create_EmptyNumber_ShouldThrow()
    {
        var act = () => Cheque.Create(TenantId, "", 1000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), "Banka", ChequeType.Received);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cheque_IsOverdue_PastMaturity_ShouldBeTrue()
    {
        var c = Cheque.Create(TenantId, "CHK-OVD", 1000m,
            DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddDays(-1),
            "Banka", ChequeType.Received);

        c.IsOverdue.Should().BeTrue("maturity passed and still InPortfolio");
    }

    [Fact]
    public void Cheque_IsOverdue_FutureMaturity_ShouldBeFalse()
    {
        var c = Cheque.Create(TenantId, "CHK-FUT", 1000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(3),
            "Banka", ChequeType.Received);

        c.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void Cheque_StatusTransitions_FullLifecycle()
    {
        var c = Cheque.Create(TenantId, "CHK-LIFE", 2000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1),
            "Is Bankasi", ChequeType.Received);

        c.Status.Should().Be(ChequeStatus.InPortfolio);
        c.SendForCollection();
        c.Status.Should().Be(ChequeStatus.SentForCollection);
        c.MarkCollected();
        c.Status.Should().Be(ChequeStatus.Collected);
    }

    [Fact]
    public void Cheque_Bounced_ShouldSetStatus()
    {
        var c = Cheque.Create(TenantId, "CHK-BOUNCE", 3000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1),
            "Halk Bankasi", ChequeType.Received);
        c.SendForCollection();
        c.MarkBounced();

        c.Status.Should().Be(ChequeStatus.Bounced);
    }

    [Fact]
    public void Cheque_Endorse_ShouldSetEndorserAndStatus()
    {
        var c = Cheque.Create(TenantId, "CHK-ENDO", 4000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(2),
            "Vakifbank", ChequeType.Received);
        c.Endorse("Mehmet Kaya");

        c.EndorserName.Should().Be("Mehmet Kaya");
        c.Status.Should().Be(ChequeStatus.Endorsed);
    }

    [Fact]
    public void Cheque_Cancel_ShouldSetStatus()
    {
        var c = Cheque.Create(TenantId, "CHK-CAN", 1500m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1),
            "Akbank", ChequeType.Given);
        c.Cancel();

        c.Status.Should().Be(ChequeStatus.Cancelled);
    }

    // ══════════════════════════════════════
    // PromissoryNote
    // ══════════════════════════════════════

    [Fact]
    public void Note_Create_Valid_ShouldSucceed()
    {
        var n = PromissoryNote.Create(TenantId, "SN-001", 10000m,
            new DateTime(2026, 3, 15), new DateTime(2026, 9, 15),
            NoteType.Received, "ABC Ticaret");

        n.NoteNumber.Should().Be("SN-001");
        n.Amount.Should().Be(10000m);
        n.Type.Should().Be(NoteType.Received);
        n.Status.Should().Be(NoteStatus.InPortfolio);
        n.DebtorName.Should().Be("ABC Ticaret");
    }

    [Fact]
    public void Note_Create_ZeroAmount_ShouldThrow()
    {
        var act = () => PromissoryNote.Create(TenantId, "SN-BAD", 0m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(6), NoteType.Given, "Borçlu");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Note_Create_EmptyDebtor_ShouldThrow()
    {
        var act = () => PromissoryNote.Create(TenantId, "SN-ND", 5000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(3), NoteType.Received, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Note_IsOverdue_PastMaturity_ShouldBeTrue()
    {
        var n = PromissoryNote.Create(TenantId, "SN-OVD", 3000m,
            DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1),
            NoteType.Received, "Borçlu");

        n.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void Note_MarkCollected_ShouldChangeStatus()
    {
        var n = PromissoryNote.Create(TenantId, "SN-COL", 7000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(3),
            NoteType.Received, "XYZ Ltd");
        n.MarkCollected();

        n.Status.Should().Be(NoteStatus.Collected);
    }

    [Fact]
    public void Note_MarkProtested_ShouldChangeStatus()
    {
        var n = PromissoryNote.Create(TenantId, "SN-PROT", 8000m,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(2),
            NoteType.Received, "Odeme Yapmayan");
        n.MarkProtested();

        n.Status.Should().Be(NoteStatus.Protested);
    }
}
