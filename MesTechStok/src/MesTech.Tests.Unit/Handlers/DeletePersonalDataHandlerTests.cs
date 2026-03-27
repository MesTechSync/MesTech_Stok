using FluentAssertions;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using Xunit;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeletePersonalDataHandlerTests
{
    [Fact]
    public void Command_ShouldBeRecord()
    {
        var cmd = typeof(DeletePersonalDataCommand);
        cmd.GetMethod("<Clone>$").Should().NotBeNull("DeletePersonalDataCommand should be a record type");
    }
}
