using FluentAssertions;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Application.Queries.GetQuotationById;
using MesTech.Application.Queries.ListQuotations;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// Task 9 gap-fill: ListQuotations, QuotationLine computed properties,
/// domain edge cases, null guards — 15 tests covering what bea7fda left open.
/// </summary>
[Trait("Category", "Unit")]
public class ListQuotationsHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();

    private ListQuotationsHandler CreateHandler() => new(_quotationRepo.Object);

    [Fact]
    public async Task NoFilter_CallsGetAllAsync_ReturnsAllDtos()
    {
        _quotationRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Quotation>
            {
                new() { QuotationNumber = "QT-001", CustomerName = "A", Currency = "TRY" },
                new() { QuotationNumber = "QT-002", CustomerName = "B", Currency = "TRY" },
                new() { QuotationNumber = "QT-003", CustomerName = "C", Currency = "TRY" },
            });

        var result = await CreateHandler().Handle(
            new ListQuotationsQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(q => q.QuotationNumber).Should().Contain("QT-001").And.Contain("QT-003");
        _quotationRepo.Verify(r => r.GetAllAsync(), Times.Once);
        _quotationRepo.Verify(r => r.GetByStatusAsync(It.IsAny<QuotationStatus>()), Times.Never);
    }

    [Fact]
    public async Task FilterByStatusDraft_CallsGetByStatusAsync_ReturnsOnlyMatching()
    {
        _quotationRepo.Setup(r => r.GetByStatusAsync(QuotationStatus.Draft))
            .ReturnsAsync(new List<Quotation>
            {
                new() { QuotationNumber = "QT-DRAFT-001", CustomerName = "Draft Co", Currency = "USD" },
            });

        var result = await CreateHandler().Handle(
            new ListQuotationsQuery(Status: QuotationStatus.Draft), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].QuotationNumber.Should().Be("QT-DRAFT-001");
        result[0].Status.Should().Be("Draft");
        _quotationRepo.Verify(r => r.GetByStatusAsync(QuotationStatus.Draft), Times.Once);
        _quotationRepo.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task EmptyRepo_ReturnsEmptyList()
    {
        _quotationRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Quotation>());

        var result = await CreateHandler().Handle(
            new ListQuotationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        _quotationRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}

[Trait("Category", "Unit")]
public class QuotationLineComputedTests
{
    [Fact]
    public void LineTotal_IsQuantityMultipliedByUnitPrice()
    {
        var line = new QuotationLine { Quantity = 5, UnitPrice = 200m, TaxRate = 18m };

        line.LineTotal.Should().Be(1000m, "5 * 200 = 1000");
    }

    [Fact]
    public void TaxAmount_IsCalculatedCorrectly()
    {
        var line = new QuotationLine { Quantity = 5, UnitPrice = 200m, TaxRate = 18m };

        // 5 * 200 * 18 / 100 = 180
        line.TaxAmount.Should().Be(180m);
    }

    [Fact]
    public void ZeroTaxRate_TaxAmountIsZero()
    {
        var line = new QuotationLine { Quantity = 10, UnitPrice = 50m, TaxRate = 0m };

        line.TaxAmount.Should().Be(0m);
        line.LineTotal.Should().Be(500m);
    }

    [Fact]
    public void ZeroQuantity_LineTotalAndTaxAmountAreZero()
    {
        var line = new QuotationLine { Quantity = 0, UnitPrice = 999m, TaxRate = 20m };

        line.LineTotal.Should().Be(0m);
        line.TaxAmount.Should().Be(0m);
    }
}

[Trait("Category", "Unit")]
public class QuotationDomainEdgeTests
{
    [Fact]
    public void MarkAsExpired_FromRejected_ChangesStatusToExpired()
    {
        // Rejected is not Accepted or Converted, so MarkAsExpired should proceed
        var q = new Quotation { QuotationNumber = "QT-REJ", CustomerName = "X" };
        q.Send();
        q.Reject();
        q.Status.Should().Be(QuotationStatus.Rejected);

        q.MarkAsExpired();

        q.Status.Should().Be(QuotationStatus.Expired);
    }

    [Fact]
    public void Send_FromRejected_ThrowsInvalidOperation()
    {
        var q = new Quotation { QuotationNumber = "QT-REJ2", CustomerName = "X" };
        q.Send();
        q.Reject();

        var act = () => q.Send();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public void Quotation_DefaultCurrency_IsTRY()
    {
        var q = new Quotation { QuotationNumber = "QT-CUR", CustomerName = "X" };

        q.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Quotation_Lines_AreManagedThroughAddLine()
    {
        var q = new Quotation { QuotationNumber = "QT-LINES", CustomerName = "X" };

        q.Lines.Should().BeEmpty();

        q.AddLine(new QuotationLine { ProductName = "P1", Quantity = 1, UnitPrice = 10m, TaxRate = 0m });
        q.AddLine(new QuotationLine { ProductName = "P2", Quantity = 2, UnitPrice = 20m, TaxRate = 0m });

        q.Lines.Should().HaveCount(2);
        q.SubTotal.Should().Be(50m, "1*10 + 2*20 = 50");
    }
}

[Trait("Category", "Unit")]
public class QuotationNullGuardTests
{
    [Fact]
    public void ListQuotationsHandler_NullRepo_ThrowsArgumentNullException()
    {
        var act = () => new ListQuotationsHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("quotationRepository");
    }

    [Fact]
    public void GetQuotationByIdHandler_NullRepo_ThrowsArgumentNullException()
    {
        var act = () => new GetQuotationByIdHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("quotationRepository");
    }

    [Fact]
    public async Task ListQuotations_FilterByAccepted_CallsGetByStatusWithCorrectEnum()
    {
        var quotationRepo = new Mock<IQuotationRepository>();
        quotationRepo.Setup(r => r.GetByStatusAsync(QuotationStatus.Accepted))
            .ReturnsAsync(new List<Quotation>
            {
                new() { QuotationNumber = "QT-ACC-001", CustomerName = "Accepted Co", Currency = "TRY",
                        Status = QuotationStatus.Accepted }
            });

        var handler = new ListQuotationsHandler(quotationRepo.Object);
        var result = await handler.Handle(
            new ListQuotationsQuery(Status: QuotationStatus.Accepted), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Accepted");
        quotationRepo.Verify(r => r.GetByStatusAsync(QuotationStatus.Accepted), Times.Once);
    }

    [Fact]
    public async Task CreateQuotationHandler_WithCustomerId_SetsCustomerIdOnQuotation()
    {
        var quotationRepo = new Mock<IQuotationRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var customerId = Guid.NewGuid();
        var command = new CreateQuotationCommand(
            QuotationNumber: "QT-CUST-001",
            ValidUntil: DateTime.UtcNow.AddDays(30),
            CustomerId: customerId,
            CustomerName: "Customer Inc");

        var handler = new CreateQuotationHandler(quotationRepo.Object, unitOfWork.Object);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        quotationRepo.Verify(r => r.AddAsync(It.Is<Quotation>(q =>
            q.CustomerId == customerId &&
            q.CustomerName == "Customer Inc"
        )), Times.Once);
    }
}
