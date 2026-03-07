using WireMock.Server;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// WireMock sunucu fixture — adapter contract testleri icin.
/// Her test class icin bir mock HTTP server baslatir.
/// </summary>
public class WireMockFixture : IDisposable
{
    public WireMockServer Server { get; }
    public string BaseUrl => Server.Url!;

    public WireMockFixture()
    {
        Server = WireMockServer.Start();
    }

    public void Reset()
    {
        Server.Reset();
    }

    public void Dispose()
    {
        Server?.Stop();
        Server?.Dispose();
    }
}
