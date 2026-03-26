using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class FetchProductFromUrlHandlerTests
{
    private readonly FetchProductFromUrlHandler _sut;

    public FetchProductFromUrlHandlerTests()
    {
        _sut = new FetchProductFromUrlHandler(Mock.Of<ILogger<FetchProductFromUrlHandler>>());
    }

    [Fact]
    public async Task Handle_EmptyUrl_ReturnsNull()
    {
        var cmd = new FetchProductFromUrlCommand("");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
