using System.Net;
using System.Text;

namespace MesTech.Integration.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> CapturedRequests => _requests.AsReadOnly();

    public void EnqueueResponse(HttpStatusCode statusCode, string jsonBody = "{}")
    {
        _responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        });
    }

    public void EnqueueResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (_responses.Count > 0)
            return Task.FromResult(_responses.Dequeue());

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
    }
}
