using Domain.Speaking;

namespace Application.Speaking.Ports;

/// <summary>Persistence for speaking conversation sessions.</summary>
public interface IConversationStore
{
    Task<ConversationSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default);
}
