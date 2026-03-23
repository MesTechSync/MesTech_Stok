using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// GetKdvDeclarationDraftHandler tests — monthly KDV (VAT) declaration draft.
/// Verifies output KDV, input KDV, withholding, carry-forward, and final payable.
/// </summary>
[Trait("Category", "Unit")]
public class GetKdvDeclarationDraftHandlerTests
{
    private readonly Mock<ITaxRecordRepository> _taxRecordRepoMock;
    private readonly Mock<ICommissionRecordRepository> _commissionRepoMock;
    private readonly Mock<ITaxWithholdingRepository> _withholdingRepoMock;
    private readonly GetKdvDeclarationDraftHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetKdvDeclarationDraftHandlerTests()
    {
        _taxRecordRepoMock = new Mock<ITaxRecordRepository>();
        _commissionRepoMock = new Mock<ICommissionRecordRepository>();
        _withholdingRepoMock = new Mock<ITaxWithholdingRepository>();

        _sut = new GetKdvDeclarationDraftHandler(
            _taxRecordRepoMock.Object,
            _commissionRepoMock.Object,
            _withholdingRepoMock.Object);
    }

    private static TaxRecord CreateTaxRecord(Guid tenantId, string period, string taxType, decimal taxAmount)
    {
        return TaxRecord.Create(tenantId, period, taxType, taxAmount / 0.20m, taxAmount,
            new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_FullKdvScenario_CalculatesCorrectPayable()
    {
        // Arrange
        var period = "2026-03";
        var taxRecords = new List<TaxRecord>
        {
            CreateTaxRecord(_tenantId, period, "KDV", 5000m),        // Sales KDV
            CreateTaxRecord(_tenantId, period, "KDV-Iade", 500m),    // Return adjustment
            CreateTaxRecord(_tenantId, period, "KDV-Alis", 1500m),   // Purchase KDV
            CreateTaxRecord(_tenantId, period, "KDV-Tevkifat", 200m),// Withholding
            CreateTaxRecord(_tenantId, period, "KDV-Devreden", 300m) // Carry-forward
        };

        _taxRecordRepoMock
            .Setup(r => r.GetByPeriodAsync(_tenantId, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRecords);

        // Commission: 1000 TL => 200 TL KDV (20%)
        _commissionRepoMock
            .Setup(r => r.GetTotalCommissionAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000m);

        var query = new GetKdvDeclarationDraftQuery(_tenantId, period);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Period.Should().Be(period);
        result.SalesKdv.Should().Be(5000m);
        result.ReturnKdvAdjustment.Should().Be(500m);
        result.NetOutputKdv.Should().Be(4500m); // 5000 - 500

        result.PurchaseKdv.Should().Be(1500m);
        result.CommissionKdv.Should().Be(200m); // 1000 * 0.20
        result.NetInputKdv.Should().Be(1700m);  // 1500 + 200

        result.WithholdingKdv.Should().Be(200m);
        result.PayableKdv.Should().Be(2600m);   // 4500 - 1700 - 200
        result.CarryForwardKdv.Should().Be(300m);
        result.FinalPayableKdv.Should().Be(2300m); // 2600 - 300

        result.ReportText.Should().NotBeNullOrEmpty();
        result.ReportText.Should().Contain("KDV BEYANNAME TASLAK");
    }

    [Fact]
    public async Task Handle_NoTaxRecords_ReturnsZeroAmounts()
    {
        // Arrange
        var period = "2026-01";

        _taxRecordRepoMock
            .Setup(r => r.GetByPeriodAsync(_tenantId, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaxRecord>());

        _commissionRepoMock
            .Setup(r => r.GetTotalCommissionAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var query = new GetKdvDeclarationDraftQuery(_tenantId, period);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.SalesKdv.Should().Be(0m);
        result.NetOutputKdv.Should().Be(0m);
        result.NetInputKdv.Should().Be(0m);
        result.PayableKdv.Should().Be(0m);
        result.FinalPayableKdv.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_NegativePayable_IndicatesCarryForward()
    {
        // Arrange — input KDV exceeds output KDV, resulting in carry-forward
        var period = "2026-02";
        var taxRecords = new List<TaxRecord>
        {
            CreateTaxRecord(_tenantId, period, "KDV", 1000m),      // Sales KDV
            CreateTaxRecord(_tenantId, period, "KDV-Alis", 3000m)  // Purchase KDV (large purchase month)
        };

        _taxRecordRepoMock
            .Setup(r => r.GetByPeriodAsync(_tenantId, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRecords);

        _commissionRepoMock
            .Setup(r => r.GetTotalCommissionAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var query = new GetKdvDeclarationDraftQuery(_tenantId, period);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — PayableKdv is negative, means devreden KDV
        result.NetOutputKdv.Should().Be(1000m);
        result.NetInputKdv.Should().Be(3000m);
        result.PayableKdv.Should().Be(-2000m);
        result.FinalPayableKdv.Should().Be(-2000m);
        result.ReportText.Should().Contain("Negatif tutar");
    }

    [Fact]
    public async Task Handle_CommissionOnlyInput_CalculatesCommissionKdv()
    {
        // Arrange — no purchase KDV, only commission KDV
        var period = "2026-03";
        var taxRecords = new List<TaxRecord>
        {
            CreateTaxRecord(_tenantId, period, "KDV-Satis", 2000m) // Alternative sales KDV type
        };

        _taxRecordRepoMock
            .Setup(r => r.GetByPeriodAsync(_tenantId, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRecords);

        _commissionRepoMock
            .Setup(r => r.GetTotalCommissionAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5000m);

        var query = new GetKdvDeclarationDraftQuery(_tenantId, period);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.SalesKdv.Should().Be(2000m);
        result.CommissionKdv.Should().Be(1000m); // 5000 * 0.20
        result.NetInputKdv.Should().Be(1000m);   // 0 + 1000
        result.PayableKdv.Should().Be(1000m);     // 2000 - 1000
    }
}
