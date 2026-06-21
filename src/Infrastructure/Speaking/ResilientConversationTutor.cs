using Application.Speaking;
using Application.Speaking.Ports;
using Domain.Speaking;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

internal sealed class ResilientConversationTutor : IConversationTutor
{
    private readonly HermesConversationTutor _primary;
    private readonly LocalConversationTutor _fallback;
    private readonly ILogger<ResilientConversationTutor> _logger;

    public ResilientConversationTutor(
        HermesConversationTutor primary,
        LocalConversationTutor fallback,
        ILogger<ResilientConversationTutor> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<string> NextReplyAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.NextReplyAsync(session, cancellationToken);
        }
        catch (SpeakingTutorUnavailableException exception)
        {
            _logger.LogWarning(
                "Speaking tutor is using the local fallback. Reason: {FailureReason}; Stage: {SpeakingStage}.",
                exception.Code,
                session.Turns.Any(turn => turn.Role == ConversationRole.Learner) ? "reply" : "opening");
            try
            {
                return await _fallback.NextReplyAsync(session, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception fallbackException)
            {
                throw new SpeakingTutorUnavailableException(exception.Code, fallbackException);
            }
        }
    }
}
