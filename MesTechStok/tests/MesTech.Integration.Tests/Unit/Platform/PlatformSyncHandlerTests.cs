using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// Platform sync handler testleri — null guard.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Platform")]
[Trait("Group", "Handler")]
public class PlatformSyncHandlerTests
{
    // ═══ TriggerSync ═══

    [Fact]
    public async Task TriggerSync_NullRequest_Throws()
    {
        var jobService = new Mock<IBackgroundJobService>();
        var logger = Mock.Of<ILogger<TriggerSyncHandler>>();
        var handler = new TriggerSyncHandler(jobService.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
