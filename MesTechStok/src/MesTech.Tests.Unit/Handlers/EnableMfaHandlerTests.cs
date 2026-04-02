using FluentAssertions;
using MesTech.Application.Features.Auth.Commands.EnableMfa;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class EnableMfaHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new EnableMfaHandler(
            Mock.Of<IUserRepository>(), Mock.Of<ITotpService>(),
            Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<EnableMfaHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<NullReferenceException>();
    }
}
