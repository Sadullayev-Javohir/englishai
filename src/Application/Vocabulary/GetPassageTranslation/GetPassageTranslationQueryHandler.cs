using Application.Common;
using Application.Subscription.Access;
using Application.Translation.Ports;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.GetPassageTranslation;

public sealed class GetPassageTranslationQueryHandler
    : IRequestHandler<GetPassageTranslationQuery, VocabularyTopicPassageTranslationDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly ITextTranslator _translator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITopicAccessPolicy _access;

    public GetPassageTranslationQueryHandler(
        IVocabularyTopicRepository topics,
        ITextTranslator translator,
        ICurrentUserAccessor currentUser,
        ITopicAccessPolicy access)
    {
        _topics = topics;
        _translator = translator;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<VocabularyTopicPassageTranslationDto> Handle(
        GetPassageTranslationQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        if (_currentUser.LearnerId is { } learnerId)
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, Domain.Learning.SkillType.Vocabulary, cancellationToken);

        var sentences = SplitSentences(topic.Passage)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var translated = new List<PassageSentenceDto>(sentences.Count);
        foreach (var sentence in sentences)
        {
            var uzbek = await _translator.TranslateAsync(sentence, topic.Level, cancellationToken: cancellationToken);
            translated.Add(new PassageSentenceDto(sentence.Trim(), uzbek?.Trim()));
        }

        return new VocabularyTopicPassageTranslationDto(translated);
    }

    // Splits on sentence boundaries (., !, ?) while keeping trailing whitespace/newlines so the
    // reader preserves paragraph breaks. Keeps the terminator attached to its sentence.
    private static IEnumerable<string> SplitSentences(string passage)
    {
        if (string.IsNullOrWhiteSpace(passage))
            yield break;

        var buffer = new System.Text.StringBuilder();
        foreach (var ch in passage)
        {
            buffer.Append(ch);
            if (ch is '.' or '!' or '?')
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
        }

        if (buffer.Length > 0)
            yield return buffer.ToString();
    }
}
