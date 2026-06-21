using Application.Common;
using Application.Speaking.Ports;
using Application.Subscription.Access;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Common;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.GetVocabularyTopic;

public sealed class GetVocabularyTopicQueryHandler
    : IRequestHandler<GetVocabularyTopicQuery, VocabularyTopicDetailDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IVocabularyPassageGenerator _generator;
    private readonly IPhonemeVisualLibrary _phonetics;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITopicAccessPolicy _access;

    public GetVocabularyTopicQueryHandler(
        IVocabularyTopicRepository topics,
        IVocabularyPassageGenerator generator,
        IPhonemeVisualLibrary phonetics,
        ICurrentUserAccessor currentUser,
        ITopicAccessPolicy access)
    {
        _topics = topics;
        _generator = generator;
        _phonetics = phonetics;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<VocabularyTopicDetailDto> Handle(
        GetVocabularyTopicQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        // Trial paywall (H.1): topics beyond the free allowance require Premium. Enforced server-side
        // so the gate cannot be bypassed by calling the API directly; skipped only when there is no
        // authenticated user (tests/background), matching LearnerOwnershipBehavior.
        if (_currentUser.LearnerId is { } learnerId)
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, Domain.Learning.SkillType.Vocabulary, cancellationToken);

        if (topic.NeedsContentRefresh)
            await TryFillAsync(topic, cancellationToken);

        var words = topic.Words
            .Select(w => new TopicWordDto(
                w.Word,
                w.Translation,
                IpaFor(w.Word),
                w.ExampleSentence,
                PartOfSpeechLabel(w.PartOfSpeech),
                w.LexicalCategory.ToString(),
                w.Register,
                w.UsageNote,
                w.ImageUrl?.StartsWith("local:", StringComparison.OrdinalIgnoreCase) == true
                    ? $"/api/images/vocabulary-topics/{topic.Id}/words/{WordImageQuery.ImageId(topic.Id, w.Word)}"
                    : null,
                w.ImageAttribution))
            .ToList();

        var quiz = topic.IsFilled
            ? topic.BuildQuiz().Select(TopicQuizQuestionDto.FromDomain).ToList()
            : new List<TopicQuizQuestionDto>();

        return new VocabularyTopicDetailDto(
            topic.Id, topic.Title, topic.TitleUz, topic.Category, topic.Level,
            topic.IsFilled, topic.Passage, words, quiz);
    }

    // Best-effort lazy fill: generate the passage/words and cache them. Any failure leaves the
    // topic honestly "pending" (IsReady=false) rather than fabricating content (rules 8, 11).
    private async Task TryFillAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _generator.GenerateAsync(
                topic.Title, topic.Level, VocabularyTopic.TargetWordCount, cancellationToken);
            if (!content.HasContent)
                return;

            var words = content.Words
                .Where(w => !string.IsNullOrWhiteSpace(w.Word) && !string.IsNullOrWhiteSpace(w.Translation))
                .Select(w => TopicWord.Create(
                    w.Word, w.Translation, w.ExampleSentence, PartOfSpeechParser.Parse(w.Pos)))
                .ToList();
            if (words.Count == 0)
                return;

            topic.FillContent(content.Passage, words);
            await _topics.SaveAsync(topic, cancellationToken);
        }
        catch (DomainException)
        {
            // Generated content failed a domain invariant - keep the topic pending.
        }
    }

    private string? IpaFor(string word) => _phonetics.GetWordPhonetics(word)?.Ipa;

    // The lowercase label the UI shows as a part-of-speech chip; Other is treated as untagged.
    private static string? PartOfSpeechLabel(PartOfSpeech pos) =>
        pos == PartOfSpeech.Other ? null : pos.ToString().ToLowerInvariant();
}
