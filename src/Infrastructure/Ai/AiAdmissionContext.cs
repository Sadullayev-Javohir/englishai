using Application.Ai;

namespace Infrastructure.Ai;

public static class AiAdmissionContext
{
    private static readonly AsyncLocal<State?> CurrentState = new();

    public static State Current => CurrentState.Value ?? State.Anonymous;

    public static IDisposable Push(State state)
    {
        var previous = CurrentState.Value;
        CurrentState.Value = state;
        return new Pop(previous);
    }

    public sealed record State(
        string CallerKey,
        AiSubscriptionTier Tier,
        AiFeature Feature,
        string? RequestPath = null)
    {
        public static State Anonymous { get; } = new("anonymous", AiSubscriptionTier.Free, AiFeature.Other);
    }

    private sealed class Pop(State? previous) : IDisposable
    {
        public void Dispose() => CurrentState.Value = previous;
    }
}
