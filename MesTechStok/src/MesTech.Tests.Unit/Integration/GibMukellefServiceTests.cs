using MesTech.Application.Interfaces;
using Xunit;

namespace MesTech.Tests.Unit.Integration;

public class GibMukellefServiceTests
{
    [Fact]
    public void IGibMukellefService_ShouldExist()
    {
        var type = typeof(IGibMukellefService);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void IGibMukellefService_ShouldHaveIsEFaturaMukellefAsync()
    {
        var method = typeof(IGibMukellefService).GetMethod("IsEFaturaMukellefAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method!.ReturnType);
    }

    [Fact]
    public void IGibMukellefService_ShouldHaveClearCacheMethod()
    {
        var method = typeof(IGibMukellefService).GetMethod("ClearCache");
        Assert.NotNull(method);
    }
}
