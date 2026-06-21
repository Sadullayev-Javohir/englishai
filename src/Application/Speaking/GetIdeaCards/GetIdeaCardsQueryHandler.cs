using Application.Ai;
using Application.Speaking.Dtos;
using Application.Speaking.Ports;
using MediatR;

namespace Application.Speaking.GetIdeaCards;

public sealed class GetIdeaCardsQueryHandler : IRequestHandler<GetIdeaCardsQuery, IdeaCardsResult>
{
    private readonly IConversationStore _conversations;
    private readonly IIdeaCardGenerator _generator;
    private readonly IAiFeatureScope _aiScope;

    public GetIdeaCardsQueryHandler(IConversationStore conversations, IIdeaCardGenerator generator, IAiFeatureScope? aiScope = null)
    {
        _conversations = conversations;
        _generator = generator;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
    }

    public async Task<IdeaCardsResult> Handle(GetIdeaCardsQuery request, CancellationToken cancellationToken)
    {
        // This is a "help me, I'm stuck" affordance during live speaking - it must never fail loudly.
        // A missing session (e.g. a stale client) or an LLM hiccup falls back to the universal
        // 5W1H/example scaffolds so the learner always gets something usable.
        var session = await _conversations.GetAsync(request.SessionId, cancellationToken);
        if (session is null)
            return ToResult(IdeaCardDefaults.Cards);

        try
        {
            using var aiScope = await _aiScope.EnterAsync(AiFeature.SpeakingTutor, session.LearnerId, cancellationToken);
            var cards = await _generator.GenerateAsync(session, cancellationToken);
            return ToResult(cards.Count == 0 ? IdeaCardDefaults.Cards : cards);
        }
        catch
        {
            return ToResult(IdeaCardDefaults.Cards);
        }
    }

    private static IdeaCardsResult ToResult(IEnumerable<Domain.Speaking.IdeaCard> cards) =>
        new(cards.Select(IdeaCardDto.FromDomain).ToList());
}
