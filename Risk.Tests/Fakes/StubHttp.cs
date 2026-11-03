using System.Net;
using System.Text;

namespace Risk.Tests.Fakes;

// A fake network: answers every request with whatever the test says, and remembers what was sent.
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;

    public int Calls { get; private set; }
    public string? Authorization { get; private set; }
    public string? Url { get; private set; }
    public string? Body { get; private set; }

    public StubHttpHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;

    public static StubHttpHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Authorization = request.Headers.Authorization?.ToString();
        Url = request.RequestUri?.ToString();
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return await _respond(cancellationToken);
    }
}

public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

    public HttpClient CreateClient(string name) =>
        new(_handler, disposeHandler: false) { BaseAddress = new Uri("https://api.mistral.ai/") };
}