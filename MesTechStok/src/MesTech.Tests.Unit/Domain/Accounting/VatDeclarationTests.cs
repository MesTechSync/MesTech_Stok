using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Domain.Accounting;

/// <summary>
/// S1-DEV5-01: VatDeclaration entity testleri.
/// Create, Calculate, Submit, Status geçişleri, edge cases.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Accounting")]
public class VatDeclarationTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public void Create_Valid_ShouldSucceed()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 3);
        vd.Year.Should().Be(2026);
        vd.Month.Should().Be(3);
        vd.Status.Should().Be(VatDeclarationStatus.Draft);
        vd.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => VatDeclaration.Create(Guid.Empty, 2026, 3);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(2019)]
    [InlineData(2100)]
    public void Create_InvalidYear_ShouldThrow(int year)
    {
        var act = () => VatDeclaration.Create(TenantId, year, 1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_InvalidMonth_ShouldThrow(int month)
    {
        var act = () => VatDeclaration.Create(TenantId, 2026, month);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculate_ShouldSetNetVatPayable()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 3);
        vd.Calculate(100000m, 18000m, 5000m);

        vd.TotalSales.Should().Be(100000m);
        vd.TotalVatCollected.Should().Be(18000m);
        vd.TotalVatPaid.Should().Be(5000m);
        vd.NetVatPayable.Should().Be(13000m, "18000 - 5000 = 13000");
        vd.Status.Should().Be(VatDeclarationStatus.Calculated);
    }

    [Fact]
    public void Calculate_NegativeNet_ShouldBeRefund()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 1);
        vd.Calculate(50000m, 2000m, 8000m);

        vd.NetVatPayable.Should().Be(-6000m, "2000 - 8000 = -6000 (iade durumu)");
    }

    [Fact]
    public void Calculate_AfterSubmit_ShouldThrow()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 2);
        vd.Calculate(10000m, 1800m, 500m);
        vd.Submit("GIB-2026-02-001");

        var act = () => vd.Calculate(20000m, 3600m, 1000m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_FromCalculated_ShouldSucceed()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 4);
        vd.Calculate(80000m, 14400m, 3000m);
        vd.Submit("GIB-REF-123");

        vd.Status.Should().Be(VatDeclarationStatus.Submitted);
        vd.GibReferenceNumber.Should().Be("GIB-REF-123");
        vd.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public void Submit_FromDraft_ShouldThrow()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 5);
        var act = () => vd.Submit("GIB-REF");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Submit_EmptyRef_ShouldThrow()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 6);
        vd.Calculate(10000m, 1800m, 500m);

        var act = () => vd.Submit("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAccepted_ShouldChangeStatus()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 7);
        vd.Calculate(10000m, 1800m, 500m);
        vd.Submit("GIB-ACC");
        vd.MarkAccepted();

        vd.Status.Should().Be(VatDeclarationStatus.Accepted);
    }

    [Fact]
    public void MarkRejected_ShouldSetReasonAndStatus()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 8);
        vd.Calculate(10000m, 1800m, 500m);
        vd.Submit("GIB-REJ");
        vd.MarkRejected("Hatali beyan");

        vd.Status.Should().Be(VatDeclarationStatus.Rejected);
        vd.Notes.Should().Be("Hatali beyan");
    }

    [Fact]
    public void StatusTransitions_FullLifecycle()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 9);
        vd.Status.Should().Be(VatDeclarationStatus.Draft);

        vd.Calculate(50000m, 9000m, 2000m);
        vd.Status.Should().Be(VatDeclarationStatus.Calculated);

        vd.Submit("GIB-LIFE");
        vd.Status.Should().Be(VatDeclarationStatus.Submitted);

        vd.MarkAccepted();
        vd.Status.Should().Be(VatDeclarationStatus.Accepted);
    }

    [Fact]
    public void Calculate_AfterAccepted_ShouldThrow()
    {
        var vd = VatDeclaration.Create(TenantId, 2026, 10);
        vd.Calculate(10000m, 1800m, 500m);
        vd.Submit("GIB-ACC2");
        vd.MarkAccepted();

        var act = () => vd.Calculate(20000m, 3600m, 1000m);
        act.Should().Throw<InvalidOperationException>();
    }
}
