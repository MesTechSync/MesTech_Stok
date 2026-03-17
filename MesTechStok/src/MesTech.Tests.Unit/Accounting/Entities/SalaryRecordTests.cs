using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class SalaryRecordTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Ahmet Yilmaz", 25000m,
            sgkEmployer: 3750m, sgkEmployee: 3500m,
            incomeTax: 3750m, stampTax: 190m,
            year: 2026, month: 3);

        record.Should().NotBeNull();
        record.EmployeeName.Should().Be("Ahmet Yilmaz");
        record.GrossSalary.Should().Be(25000m);
        record.SGKEmployer.Should().Be(3750m);
        record.SGKEmployee.Should().Be(3500m);
        record.IncomeTax.Should().Be(3750m);
        record.StampTax.Should().Be(190m);
        record.Year.Should().Be(2026);
        record.Month.Should().Be(3);
    }

    [Fact]
    public void Create_ShouldCalculateNetSalary()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test Employee", 20000m,
            sgkEmployer: 3000m, sgkEmployee: 2800m,
            incomeTax: 3000m, stampTax: 152m,
            year: 2026, month: 1);

        // NetSalary = Gross - SGKEmployee - IncomeTax - StampTax
        var expectedNet = 20000m - 2800m - 3000m - 152m;
        record.NetSalary.Should().Be(expectedNet);
    }

    [Fact]
    public void Create_ShouldCalculateTotalEmployerCost()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test Employee", 20000m,
            sgkEmployer: 3000m, sgkEmployee: 2800m,
            incomeTax: 3000m, stampTax: 152m,
            year: 2026, month: 1);

        // TotalEmployerCost = GrossSalary + SGKEmployer
        record.TotalEmployerCost.Should().Be(20000m + 3000m);
    }

    [Fact]
    public void Create_ShouldSetPaymentStatusToPending()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        record.PaymentStatus.Should().Be(PaymentStatus.Pending);
        record.PaidDate.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => SalaryRecord.Create(
            _tenantId, "", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => SalaryRecord.Create(
            _tenantId, null!, 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroGrossSalary_ShouldThrow()
    {
        var act = () => SalaryRecord.Create(
            _tenantId, "Test", 0m,
            sgkEmployer: 0m, sgkEmployee: 0m,
            incomeTax: 0m, stampTax: 0m,
            year: 2026, month: 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNegativeGrossSalary_ShouldThrow()
    {
        var act = () => SalaryRecord.Create(
            _tenantId, "Test", -5000m,
            sgkEmployer: 0m, sgkEmployee: 0m,
            incomeTax: 0m, stampTax: 0m,
            year: 2026, month: 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Create_WithInvalidYear_ShouldThrow(int year)
    {
        var act = () => SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: year, month: 3);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_WithInvalidMonth_ShouldThrow(int month)
    {
        var act = () => SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: month);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsPaid_ShouldSetStatusAndDate()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        record.MarkAsPaid();

        record.PaymentStatus.Should().Be(PaymentStatus.Completed);
        record.PaidDate.Should().NotBeNull();
        record.PaidDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsPaid_WithSpecificDate_ShouldUseThatDate()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        var specificDate = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        record.MarkAsPaid(specificDate);

        record.PaidDate.Should().Be(specificDate);
    }

    [Fact]
    public void MarkAsPaid_ShouldUpdateUpdatedAt()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        record.MarkAsPaid();

        record.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdatePaymentStatus_ShouldSetNewStatus()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        record.UpdatePaymentStatus(PaymentStatus.Processing);

        record.PaymentStatus.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var r1 = SalaryRecord.Create(_tenantId, "Employee A", 10000m,
            sgkEmployer: 1500m, sgkEmployee: 1400m,
            incomeTax: 1500m, stampTax: 76m, year: 2026, month: 1);
        var r2 = SalaryRecord.Create(_tenantId, "Employee B", 12000m,
            sgkEmployer: 1800m, sgkEmployee: 1680m,
            incomeTax: 1800m, stampTax: 91m, year: 2026, month: 1);

        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3);

        record.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Create_WithOptionalEmployeeId_ShouldSet()
    {
        var empId = Guid.NewGuid();
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3, employeeId: empId);

        record.EmployeeId.Should().Be(empId);
    }

    [Fact]
    public void Create_WithOptionalNotes_ShouldSet()
    {
        var record = SalaryRecord.Create(
            _tenantId, "Test", 15000m,
            sgkEmployer: 2250m, sgkEmployee: 2100m,
            incomeTax: 2250m, stampTax: 114m,
            year: 2026, month: 3, notes: "Ek mesai dahil");

        record.Notes.Should().Be("Ek mesai dahil");
    }
}
