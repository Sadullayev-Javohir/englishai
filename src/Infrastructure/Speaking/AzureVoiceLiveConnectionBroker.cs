using Application.Speaking.AccentTutors;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

/// <summary>
/// Mints a direct-browser Azure Voice Live connection for an accent tutor: a short-lived Entra
/// token (via the same service principal the agent uses) plus the realtime WebSocket URL that
/// attaches the existing Foundry agent, and the per-accent session config. The browser opens the
/// socket straight to Azure and passes the token as the <c>authorization</c> query parameter
/// (Voice Live accepts a bearer token there, since browsers cannot set WebSocket headers).
/// </summary>
public sealed class AzureVoiceLiveConnectionBroker : IVoiceLiveConnectionBroker
{
    private const string AuthorizationQueryParameter = "authorization";
    private const string AudioFormat = "pcm16";

    private readonly AzureAccentTutorOptions _tutors;
    private readonly AzureVoiceLiveOptions _options;
    private readonly ILogger<AzureVoiceLiveConnectionBroker> _logger;
    private readonly Lazy<TokenCredential>? _credential;

    public AzureVoiceLiveConnectionBroker(
        AzureAccentTutorOptions tutors,
        AzureVoiceLiveOptions options,
        ILogger<AzureVoiceLiveConnectionBroker> logger)
    {
        _tutors = tutors;
        _options = options;
        _logger = logger;
        if (tutors.IsConfigured)
            _credential = new Lazy<TokenCredential>(() => CreateCredential(tutors, logger));
    }

    public bool IsConfigured => _credential is not null;

    public async Task<VoiceLiveConnectionResult> CreateAsync(string tutorId, CancellationToken cancellationToken = default)
    {
        if (_credential is null)
            throw new InvalidOperationException("Voice Live is not configured.");
        if (!_tutors.Tutors.TryGetValue(tutorId, out var tutor))
            throw new ArgumentException("Unknown accent tutor.", nameof(tutorId));

        var (host, project) = ParseEndpoint(_tutors.Endpoint);
        var url = BuildWebSocketUrl(host, _options.ApiVersion, tutor.AgentName, project);

        var token = await _credential.Value
            .GetTokenAsync(new TokenRequestContext([_options.TokenScope]), cancellationToken)
            .ConfigureAwait(false);

        return new VoiceLiveConnectionResult(
            url,
            AuthorizationQueryParameter,
            $"Bearer {token.Token}",
            token.ExpiresOn,
            new VoiceLiveSessionConfig(
                tutor.VoiceName,
                AudioFormat,
                AudioFormat,
                _options.InputSamplingRate,
                _options.SilenceDurationMs,
                _options.TurnDetectionType));
    }

    // Mirrors AzureAccentTutorAgent.CreateCredential so the token comes from the same identity.
    private static TokenCredential CreateCredential(AzureAccentTutorOptions tutors, ILogger logger)
    {
        if (tutors.HasServicePrincipal)
        {
            logger.LogInformation("Voice Live is authenticating with a service principal.");
            return new ClientSecretCredential(tutors.TenantId, tutors.ClientId, tutors.ClientSecret);
        }
        logger.LogInformation("Voice Live is authenticating with DefaultAzureCredential.");
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true });
    }

    // Endpoint is like https://<resource>.services.ai.azure.com/api/projects/<project>. Pure/testable.
    internal static (string Host, string Project) ParseEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint, UriKind.Absolute);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var index = Array.FindIndex(segments, s => s.Equals("projects", StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index + 1 >= segments.Length)
            throw new InvalidOperationException($"Accent tutor endpoint has no project segment: {endpoint}");
        return (uri.Host, Uri.UnescapeDataString(segments[index + 1]));
    }

    // wss://<host>/voice-live/realtime?api-version=..&agent-name=..&agent-project-name=..  Pure/testable.
    internal static string BuildWebSocketUrl(string host, string apiVersion, string agentName, string project) =>
        $"wss://{host}/voice-live/realtime"
        + $"?api-version={Uri.EscapeDataString(apiVersion)}"
        + $"&agent-name={Uri.EscapeDataString(agentName)}"
        + $"&agent-project-name={Uri.EscapeDataString(project)}";
}
