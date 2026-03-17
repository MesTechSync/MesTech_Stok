using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;
using MesTech.Application.Features.Accounting.Queries.GetKdvReport;
using MediatR;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// GetKdvReportHandler tests — KDV raporu dogrulama ve son tarih hesabi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetKdvReportHandlerTests
{
    private readonly Mock<ISender> _mediatorMock;
    private readonly GetKdvReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetKdvReportHandlerTests()
    {
        _mediatorMock = new Mock<ISender>();
        _sut = new GetKdvReportHandler(_mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsKdvReportDtoWithCorrectDeadline()
    {
        // Arrange
        var query = new GetKdvReportQuery(_tenantId, Year: 2026, Month: 3);

        var draftDto = new KdvDeclarationDraftDto
        {
            Period = "2026-03",
            NetOutputKdv = 1800m,
            NetInputKdv = 500m,
            FinalPayableKdv = 1300m
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetKdvDeclarationDraftQuery>(q =>
                    q.TenantId == _tenantId && q.Period == "2026-03"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftDto);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
        result.HesaplananKdv.Should().Be(1800m);
        result.IndirilecekKdv.Should().Be(500m);
        result.OdenecekKdv.Should().Be(1300m);

        // Beyanname son tarihi: Nisan 2026'nin 26'si
        result.BeyannameSonTarih.Year.Should().Be(2026);
        result.BeyannameSonTarih.Month.Should().Be(4);
        result.BeyannameSonTarih.Day.Should().Be(26);
    }

    [Fact]
    public async Task Handle_DecemberQuery_DeadlineIsJanuary26NextYear()
    {
        // Arrange
        var query = new GetKdvReportQuery(_tenantId, Year: 2025, Month: 12);

        var draftDto = new KdvDeclarationDraftDto
        {
            Period = "2025-12",
            NetOutputKdv = 2000m,
            NetInputKdv = 800m,
            FinalPayableKdv = 1200m
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetKdvDeclarationDraftQuery>(q =>
                    q.TenantId == _tenantId && q.Period == "2025-12"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftDto);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.BeyannameSonTarih.Year.Should().Be(2026);
        result.BeyannameSonTarih.Month.Should().Be(1);
        result.BeyannameSonTarih.Day.Should().Be(26);
    }
}
