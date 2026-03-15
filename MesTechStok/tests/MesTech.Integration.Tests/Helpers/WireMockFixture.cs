using WireMock.Server;

namespace MesTech.Integration.Tests.Helpers;

public class WireMockFixture : IDisposable
{
    public WireMockServer Server { get; }
    public string ServerUrl => Server.Url!;

    public WireMockFixture()
    {
        Server = WireMockServer.Start();
    }

    public void Dispose()
    {
        Server.Stop();
        Server.Dispose();
    }
}
