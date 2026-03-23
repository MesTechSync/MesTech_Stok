using FluentAssertions;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Platform;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SyncBitrix24ContactsValidatorTests
{
    private readonly SyncBitrix24ContactsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new SyncBitrix24ContactsCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
