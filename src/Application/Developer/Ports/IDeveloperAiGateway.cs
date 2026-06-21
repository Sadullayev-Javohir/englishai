using System.Text.Json;

namespace Application.Developer.Ports;

public interface IDeveloperAiGateway
{
    string Model { get; }
    Task<DeveloperAiGatewayResponse> SendChatCompletionAsync(
        JsonElement request,
        CancellationToken cancellationToken);
}

public sealed class DeveloperAiGatewayResponse : IAsyncDisposable
{
    public DeveloperAiGatewayResponse(HttpResponseMessage response) => Response = response;

    public HttpResponseMessage Response { get; }

    public ValueTask DisposeAsync()
    {
        Response.Dispose();
        return ValueTask.CompletedTask;
    }
}
