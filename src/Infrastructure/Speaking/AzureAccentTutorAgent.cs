using Application.Speaking;
using System.Text;
using Application.Speaking.AccentTutors;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Azure.AI.Projects;
using Azure.AI.Extensions.OpenAI;
using Azure;
using Azure.Core;
using Azure.Identity;
using OpenAI.Responses;
using Microsoft.Extensions.Logging;

#pragma warning disable OPENAI001

namespace Infrastructure.Speaking;

public sealed class AzureAccentTutorAgent : IAccentTutorAgent
{
    private readonly AzureAccentTutorOptions _options;
    private readonly ILogger<AzureAccentTutorAgent> _logger;
    private readonly Lazy<AIProjectClient>? _client;

    public AzureAccentTutorAgent(
        AzureAccentTutorOptions options,
        ILogger<AzureAccentTutorAgent> logger)
    {
        _options = options;
        _logger = logger;
        if (options.IsConfigured)
        {
            _client = new Lazy<AIProjectClient>(() => new AIProjectClient(
                new Uri(options.Endpoint),
                CreateCredential(options, logger)));
        }
    }

    internal static TokenCredential CreateCredential(
        AzureAccentTutorOptions options,
        ILogger<AzureAccentTutorAgent> logger)
    {
        if (options.HasServicePrincipal)
        {
            logger.LogInformation("Azure accent tutor is authenticating with a service principal.");
            return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        }

        // No explicit service principal: rely on the ambient identity (managed identity in prod,
        // az login in dev). Exclude the interactive/browser flows so token acquisition fails fast
        // instead of hanging a live turn while probing unavailable credential sources.
        logger.LogInformation("Azure accent tutor is authenticating with DefaultAzureCredential.");
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeInteractiveBrowserCredential = true,
        });
    }

    public bool IsConfigured => _client is not null;

    public async Task<string> ReplyAsync(
        string tutorId,
        IReadOnlyList<AccentTutorMessage> history,
        string learnerText,
        CancellationToken cancellationToken = default)
    {
        if (_client is null || !_options.Tutors.TryGetValue(tutorId, out var tutor))
            throw new SpeakingTutorUnavailableException("accent_tutor_not_configured", retryable: false);

        var prompt = BuildPrompt(history, learnerText);
        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var reference = new AgentReference(tutor.AgentName, tutor.AgentVersion);
                var responses = _client.Value.OpenAI.GetProjectResponsesClientForAgent(reference);
                ResponseResult result = responses.CreateResponse(prompt);
                return result.GetOutputText();
            }, cancellationToken);
        }
        catch (AuthenticationFailedException exception)
        {
            _logger.LogError(exception, "Azure accent tutor authentication failed for {TutorId}.", tutorId);
            throw new SpeakingTutorUnavailableException("accent_tutor_authentication_failed", innerException: exception);
        }
        catch (RequestFailedException exception) when (exception.Status is 401 or 403)
        {
            _logger.LogError(exception, "Azure accent tutor authorization failed for {TutorId}.", tutorId);
            throw new SpeakingTutorUnavailableException("accent_tutor_authentication_failed", innerException: exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Azure accent tutor provider failed for {TutorId}.", tutorId);
            throw new SpeakingTutorUnavailableException("accent_tutor_provider_unavailable", innerException: exception);
        }
    }

    private static string BuildPrompt(
        IReadOnlyList<AccentTutorMessage> history,
        string learnerText)
    {
        var prompt = new StringBuilder(
            "Continue a live voice lesson. Reply in spoken English only, in 1-3 short sentences. " +
            "Stay in your assigned tutor persona and accent. Correct the learner naturally and ask at most one question. " +
            "Do not use markdown, lists, emoji, phonetic symbols, or stage directions.\n\n");

        if (history.Count > 0)
        {
            prompt.AppendLine("Recent conversation:");
            foreach (var message in history)
            {
                prompt.Append(message.Role == "tutor" ? "Tutor: " : "Learner: ")
                    .AppendLine(message.Text);
            }
            prompt.AppendLine();
        }

        prompt.Append("Learner: ").AppendLine(learnerText).Append("Tutor:");
        return prompt.ToString();
    }
}

public sealed class AccentTutorVoiceService(
    AzureAccentTutorOptions options,
    ITextToSpeechService fallback) : IAccentTutorVoiceService
{
    public Task<SynthesizedSpeech> SynthesizeAsync(
        string tutorId,
        string text,
        CancellationToken cancellationToken = default)
    {
        // Ask for the capability, never for a concrete adapter: the registered service is wrapped by
        // a cache, and a type check against the Azure class would silently drop every tutor back to
        // the default voice - the whole point of the four accent tutors.
        if (fallback is IVoicedTextToSpeechService voiced &&
            options.Tutors.TryGetValue(tutorId, out var tutor))
            return voiced.SynthesizeWithVoiceAsync(text, tutor.VoiceName, cancellationToken);

        return fallback.SynthesizeAsync(text, cancellationToken);
    }
}
