using FluentAssertions;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Features.Returns;

[Trait("Category", "Unit")]
public class GetReturnListHandlerTests
{
    private readonly Mock<IReturnRequestRepository> _returnRepoMock = new();
    private readonly GetReturnListHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetReturnListHandlerTests()
        => _sut = new GetReturnListHandler(_returnRepoMock.Object);

    [Fact]
    public async Task Handle_ReturnsMappedDtos()
    {
        var returns = new List<ReturnRequest>
        {
            CreateReturnWithRefund(_tenantId, 150m),
        };
        _returnRepoMock.Setup(r => r.GetByTenantAsync(_tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returns);

        var result = await _sut.Handle(new GetReturnListQuery(_tenantId, 20), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].RefundAmount.Should().Be(150m);
    }

    [Fact]
    public async Task Handle_EmptyReturns_ReturnsEmptyList()
    {
        _returnRepoMock.Setup(r => r.GetByTenantAsync(_tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReturnRequest>());

        var result = await _sut.Handle(new GetReturnListQuery(_tenantId, 20), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static ReturnRequest CreateReturnWithRefund(Guid tenantId, decimal refundAmount)
    {
        var ret = ReturnRequest.Create(
            Guid.NewGuid(), tenantId, PlatformType.Trendyol,
            ReturnReason.DefectiveProduct, "Test Customer");
        ret.AddLine(new ReturnRequestLine
        {
            TenantId = tenantId,
            ProductName = "Test Product",
            Quantity = 1,
            UnitPrice = refundAmount,
            RefundAmount = refundAmount
        });
        return ret;
    }
}
