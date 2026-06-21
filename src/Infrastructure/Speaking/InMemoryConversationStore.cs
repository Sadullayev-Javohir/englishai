using System.Collections.Concurrent;
using Application.Speaking.Ports;
using Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>Thread-safe in-memory conversation store for Faza 1 (durable later).</summary>
public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, ConversationSession> _sessions = new();

    public Task<ConversationSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task SaveAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
