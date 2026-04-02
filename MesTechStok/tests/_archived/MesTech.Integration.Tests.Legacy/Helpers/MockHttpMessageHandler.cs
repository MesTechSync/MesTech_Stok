using System.Net;
using System.Text;

namespace MesTech.Integration.Tests.Helpers;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();
    private readonly List<string?> _requestBodies = new();

    public IReadOnlyList<HttpRequestMessage> CapturedRequests => _requests.AsReadOnly();

    /// <summary>
    /// Request body strings captured before the request is potentially disposed
    /// by adapter code using 'using var request'. Safe to read after disposal.
    /// </summary>
    public IReadOnlyList<string?> CapturedRequestBodies => _requestBodies.AsReadOnly();

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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Capture body before the request may be disposed by caller's 'using' statement
        string? body = null;
        if (request.Content is not null)
        {
            try
            {
                body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Content already disposed — skip
            }
        }

        _requests.Add(request);
        _requestBodies.Add(body);

        if (_responses.Count > 0)
            return _responses.Dequeue();

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}
