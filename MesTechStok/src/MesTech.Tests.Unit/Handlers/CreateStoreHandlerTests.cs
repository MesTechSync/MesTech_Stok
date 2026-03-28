using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateStoreHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new CreateStoreHandler(
            Mock.Of<IStoreRepository>(), Mock.Of<IStoreCredentialRepository>(),
            Mock.Of<ICredentialEncryptionService>(), Mock.Of<IAdapterFactory>(),
            Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<CreateStoreHandler>>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_EmptyStoreName_ReturnsFail()
    {
        var sut = new CreateStoreHandler(
            Mock.Of<IStoreRepository>(), Mock.Of<IStoreCredentialRepository>(),
            Mock.Of<ICredentialEncryptionService>(), Mock.Of<IAdapterFactory>(),
            Mock.Of<IUnitOfWork>(), Mock.Of<ILogger<CreateStoreHandler>>());

        var cmd = new CreateStoreCommand(Guid.NewGuid(), "", MesTech.Domain.Enums.PlatformType.Trendyol, new Dictionary<string, string>());
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Store name");
    }
}
