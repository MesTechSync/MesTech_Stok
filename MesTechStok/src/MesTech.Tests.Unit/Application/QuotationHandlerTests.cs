using FluentAssertions;
using MesTech.Application.Commands.AcceptQuotation;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Application.Commands.RejectQuotation;
using MesTech.Application.Queries.GetQuotationById;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ════════════════════════════════════════════════════════
// Task 19: Quotation CQRS Handler Tests
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
public class CreateQuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateQuotationHandler CreateHandler() =>
        new(_quotationRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ValidCommand_CreatesQuotationAndReturnsSuccess()
    {
        // Arrange
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateQuotationCommand(
            QuotationNumber: "QT-2026-001",
            ValidUntil: DateTime.UtcNow.AddDays(30),
            CustomerName: "Acme Corp");

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.QuotationId.Should().NotBeEmpty();
        _quotationRepo.Verify(r => r.AddAsync(It.Is<Quotation>(q =>
            q.QuotationNumber == "QT-2026-001" &&
            q.CustomerName == "Acme Corp"
        )), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithLines_AddsLinesToQuotation()
    {
        // Arrange
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var lines = new List<CreateQuotationLineInput>
        {
            new(ProductId: Guid.NewGuid(), ProductName: "Widget A", SKU: "WA-001",
                Quantity: 5, UnitPrice: 100m, TaxRate: 18m),
            new(ProductId: Guid.NewGuid(), ProductName: "Widget B", SKU: "WB-002",
                Quantity: 3, UnitPrice: 50m, TaxRate: 8m),
        };

        var command = new CreateQuotationCommand(
            QuotationNumber: "QT-2026-002",
            ValidUntil: DateTime.UtcNow.AddDays(15),
            CustomerName: "Test Customer",
            Lines: lines);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _quotationRepo.Verify(r => r.AddAsync(It.Is<Quotation>(q =>
            q.Lines.Count == 2 &&
            q.SubTotal > 0
        )), Times.Once);
    }
}

[Trait("Category", "Unit")]
public class AcceptQuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private AcceptQuotationHandler CreateHandler() =>
        new(_quotationRepo.Object, _unitOfWork.Object);

    private static Quotation CreateSentQuotation(Guid? id = null)
    {
        var q = new Quotation
        {
            QuotationNumber = "QT-ACCEPT-001",
            CustomerName = "Accept Test",
        };
        if (id.HasValue)
            EntityTestHelper.SetEntityId(q, id.Value);
        q.Send(); // Draft → Sent
        return q;
    }

    [Fact]
    public async Task Handle_SentQuotation_AcceptsAndReturnsSuccess()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        var quotation = CreateSentQuotation(quotationId);

        _quotationRepo.Setup(r => r.GetByIdAsync(quotationId))
            .ReturnsAsync(quotation);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new AcceptQuotationCommand(quotationId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        quotation.Status.Should().Be(QuotationStatus.Accepted);
        _quotationRepo.Verify(r => r.UpdateAsync(quotation), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        _quotationRepo.Setup(r => r.GetByIdAsync(quotationId))
            .ReturnsAsync((Quotation?)null);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new AcceptQuotationCommand(quotationId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DraftQuotation_ReturnsError()
    {
        // Arrange — quotation in Draft status (Accept requires Sent)
        var quotationId = Guid.NewGuid();
        var quotation = new Quotation
        {
            QuotationNumber = "QT-DRAFT",
            CustomerName = "Draft Customer",
        };
        EntityTestHelper.SetEntityId(quotation, quotationId);

        _quotationRepo.Setup(r => r.GetByIdAsync(quotationId))
            .ReturnsAsync(quotation);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new AcceptQuotationCommand(quotationId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Sent");
    }
}

[Trait("Category", "Unit")]
public class RejectQuotationHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private RejectQuotationHandler CreateHandler() =>
        new(_quotationRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_SentQuotation_RejectsAndReturnsSuccess()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        var quotation = new Quotation
        {
            QuotationNumber = "QT-REJECT-001",
            CustomerName = "Reject Test",
        };
        EntityTestHelper.SetEntityId(quotation, quotationId);
        quotation.Send(); // Draft → Sent

        _quotationRepo.Setup(r => r.GetByIdAsync(quotationId))
            .ReturnsAsync(quotation);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new RejectQuotationCommand(quotationId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        quotation.Status.Should().Be(QuotationStatus.Rejected);
        _quotationRepo.Verify(r => r.UpdateAsync(quotation), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        _quotationRepo.Setup(r => r.GetByIdAsync(quotationId))
            .ReturnsAsync((Quotation?)null);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new RejectQuotationCommand(quotationId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}

[Trait("Category", "Unit")]
public class ConvertQuotationToInvoiceHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ConvertQuotationToInvoiceHandler CreateHandler() =>
        new(_quotationRepo.Object, _invoiceRepo.Object, _unitOfWork.Object);

    private static Quotation CreateAcceptedQuotationWithLines(Guid? id = null)
    {
        var q = new Quotation
        {
            QuotationNumber = "QT-CONV-001",
            CustomerName = "Convert Customer",
            CustomerTaxNumber = "1234567890",
            CustomerAddress = "Test Address",
            Currency = "TRY",
        };
        if (id.HasValue)
            EntityTestHelper.SetEntityId(q, id.Value);

        // Add lines
        q.AddLine(new QuotationLine
        {
            ProductName = "Product A",
            SKU = "PA-001",
            Quantity = 2,
            UnitPrice = 100m,
            TaxRate = 18m,
        });
        q.AddLine(new QuotationLine
        {
            ProductName = "Product B",
            SKU = "PB-002",
            Quantity = 1,
            UnitPrice = 250m,
            TaxRate = 8m,
        });

        // Draft → Sent → Accepted
        q.Send();
        q.Accept();
        return q;
    }

    [Fact]
    public async Task Handle_AcceptedQuotation_CreatesInvoiceAndConverts()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        var quotation = CreateAcceptedQuotationWithLines(quotationId);

        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotationId))
            .ReturnsAsync(quotation);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new ConvertQuotationToInvoiceCommand(quotationId, "INV-2026-001"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.InvoiceId.Should().NotBeEmpty();
        quotation.Status.Should().Be(QuotationStatus.Converted);
        quotation.ConvertedInvoiceId.Should().Be(result.InvoiceId);
        _invoiceRepo.Verify(r => r.AddAsync(It.Is<MesTech.Domain.Entities.Invoice>(inv =>
            inv.InvoiceNumber == "INV-2026-001" &&
            inv.CustomerName == "Convert Customer"
        )), Times.Once);
        _quotationRepo.Verify(r => r.UpdateAsync(quotation), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotationId))
            .ReturnsAsync((Quotation?)null);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new ConvertQuotationToInvoiceCommand(quotationId, "INV-X"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}

[Trait("Category", "Unit")]
public class GetQuotationByIdHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();

    private GetQuotationByIdHandler CreateHandler() =>
        new(_quotationRepo.Object);

    [Fact]
    public async Task Handle_ExistingQuotation_ReturnsDto()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        var quotation = new Quotation
        {
            QuotationNumber = "QT-GET-001",
            CustomerName = "Query Customer",
            ValidUntil = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Currency = "TRY",
            Notes = "Test notes",
        };
        EntityTestHelper.SetEntityId(quotation, quotationId);

        quotation.AddLine(new QuotationLine
        {
            ProductName = "Test Product",
            SKU = "TP-001",
            Quantity = 3,
            UnitPrice = 100m,
            TaxRate = 18m,
        });

        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotationId))
            .ReturnsAsync(quotation);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetQuotationByIdQuery(quotationId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(quotationId);
        result.QuotationNumber.Should().Be("QT-GET-001");
        result.CustomerName.Should().Be("Query Customer");
        result.Status.Should().Be("Draft");
        result.Currency.Should().Be("TRY");
        result.Notes.Should().Be("Test notes");
        result.Lines.Should().HaveCount(1);
        result.Lines[0].ProductName.Should().Be("Test Product");
        result.Lines[0].Quantity.Should().Be(3);
        result.Lines[0].UnitPrice.Should().Be(100m);
        result.SubTotal.Should().Be(300m); // 3 * 100
        result.TaxTotal.Should().Be(54m);  // 3 * 100 * 18/100
        result.GrandTotal.Should().Be(354m);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        // Arrange
        var quotationId = Guid.NewGuid();
        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotationId))
            .ReturnsAsync((Quotation?)null);

        // Act
        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetQuotationByIdQuery(quotationId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
