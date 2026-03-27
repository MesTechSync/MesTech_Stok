using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ExportPersonalDataHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new ExportPersonalDataHandler(
            Mock.Of<ITenantRepository>(), Mock.Of<IStoreRepository>(),
            Mock.Of<IOrderRepository>(), Mock.Of<IProductRepository>(),
            Mock.Of<IUserRepository>(), Mock.Of<IKvkkAuditLogRepository>(),
            Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<ExportPersonalDataHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
