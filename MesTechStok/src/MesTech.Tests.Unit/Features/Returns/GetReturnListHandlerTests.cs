using FluentAssertions;
using MesTech.Application.Features.Returns.Queries.GetReturnList;
using MesTech.Domain.Entities;
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
            ReturnRequest.Create(_tenantId, Guid.NewGuid(), MesTech.Domain.Enums.ReturnReason.Defective, 150m),
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
}
