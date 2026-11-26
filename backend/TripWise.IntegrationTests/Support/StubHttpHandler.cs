using System.Net;
using System.Text;

namespace TripWise.IntegrationTests.Support;

public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();
    private Func<HttpResponseMessage> _fallback = () => new HttpResponseMessage(HttpStatusCode.NotFound);

    public List<RecordedRequest> Requests { get; } = [];

    public void RespondAlways(HttpStatusCode status, string body) => _fallback = () => Response(status, body);

    public void RespondOnce(HttpStatusCode status, string body) => _responses.Enqueue(() => Response(status, body));

    public void ThrowAlways(Exception exception) => _fallback = () => throw exception;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value));
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.ToString(), headers, body));

        return _responses.Count > 0 ? _responses.Dequeue()() : _fallback();
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };
}

public sealed record RecordedRequest(HttpMethod Method, string Uri, Dictionary<string, string> Headers, string? Body);
