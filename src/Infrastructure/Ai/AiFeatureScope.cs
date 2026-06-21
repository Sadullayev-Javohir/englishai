using Application.Ai;

namespace Infrastructure.Ai;

public sealed class AiFeatureScope(IAiRequestContextResolver contexts) : IAiFeatureScope
{
    public async Task<IDisposable> EnterAsync(AiFeature feature, Guid? learnerId, CancellationToken cancellationToken)
    {
        var current = AiAdmissionContext.Current;
        var callerKey = learnerId?.ToString()
                        ?? (ReferenceEquals(current, AiAdmissionContext.State.Anonymous) ? null : current.CallerKey);
        var context = await contexts.ResolveAsync(feature, callerKey, cancellationToken);
        return AiAdmissionContext.Push(new(
            context.CallerKey,
            context.Tier,
            context.Feature,
            current.RequestPath));
    }
}
