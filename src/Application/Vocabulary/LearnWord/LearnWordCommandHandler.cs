using Application.Subscription.Entitlements;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Subscription;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.LearnWord;

public sealed class LearnWordCommandHandler : IRequestHandler<LearnWordCommand, VocabularyItemDto>
{
    private readonly IVocabularyRepository _vocabulary;
    private readonly IEntitlementService _entitlements;
    private readonly TimeProvider _clock;

    public LearnWordCommandHandler(
        IVocabularyRepository vocabulary, IEntitlementService entitlements, TimeProvider clock)
    {
        _vocabulary = vocabulary;
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<VocabularyItemDto> Handle(LearnWordCommand request, CancellationToken cancellationToken)
    {
        // Freemium gating (PROJECT-SPEC H.1): Free learners may add a limited number of new
        // SRS words per day (reviews stay unlimited). Premium is unlimited.
        await _entitlements.EnsureAllowedAsync(
            request.LearnerId, PremiumFeature.NewVocabularyWord, cancellationToken);

        var item = VocabularyItem.Learn(
            request.LearnerId,
            request.Word,
            request.Translation,
            _clock.GetUtcNow(),
            request.ExampleSentence,
            request.Source);

        await _vocabulary.SaveAsync(item, cancellationToken);
        await _entitlements.RecordUsageAsync(
            request.LearnerId, PremiumFeature.NewVocabularyWord, cancellationToken);

        return VocabularyItemDto.FromDomain(item);
    }
}
