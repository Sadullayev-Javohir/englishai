using Application.Common;
using Application.Reading.Dtos;
using Application.Reading.Models;
using Application.Reading.Ports;
using Application.Subscription.Access;
using Application.Vocabulary;
using Application.Vocabulary.Ports;
using Domain.Common;
using Domain.Reading;
using Domain.Vocabulary;
using MediatR;

namespace Application.Reading.GetReadingPassage;

/// <summary>
/// Resolves a topic's reading lesson, generating and caching it on first open - the same lazy-fill
/// pattern as a vocabulary topic (<c>GetVocabularyTopicQueryHandler</c>). A failed generation
/// leaves the lesson honestly pending (<c>IsReady=false</c>) rather than fabricating content.
/// </summary>
public sealed class GetReadingPassageQueryHandler : IRequestHandler<GetReadingPassageQuery, ReadingPassageDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IReadingRepository _passages;
    private readonly IReadingContentGenerator _generator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITopicAccessPolicy _access;

    public GetReadingPassageQueryHandler(
        IVocabularyTopicRepository topics,
        IReadingRepository passages,
        IReadingContentGenerator generator,
        ICurrentUserAccessor currentUser,
        ITopicAccessPolicy access)
    {
        _topics = topics;
        _passages = passages;
        _generator = generator;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<ReadingPassageDto> Handle(GetReadingPassageQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        // Trial paywall (H.1): topics beyond the free allowance require Premium (server-side enforce).
        if (_currentUser.LearnerId is { } learnerId)
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, Domain.Learning.SkillType.Reading, cancellationToken);

        var passage = await _passages.GetByTopicIdAsync(topic.Id, cancellationToken);

        if (passage is null || !passage.IsFilled)
            passage = await TryGenerateAsync(topic, passage, cancellationToken);

        return passage is { IsFilled: true }
            ? ReadingPassageDto.FromDomain(topic, passage)
            : ReadingPassageDto.Pending(topic);
    }

    // Best-effort lazy fill: generate the body/glossary/questions and cache them. Any failure
    // leaves the lesson honestly "pending" rather than fabricating content (rules 8, 11).
    private async Task<ReadingPassage?> TryGenerateAsync(
        VocabularyTopic topic, ReadingPassage? existing, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _generator.GenerateAsync(
                topic.Title, topic.Level, TopicTargetWords.Of(topic), cancellationToken);
            if (!content.HasContent)
                return existing;

            var questions = content.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Prompt) && q.Options.Count >= ReadingQuestion.MinOptions)
                .Select(q => ReadingQuestion.Create(q.Prompt, q.Options, q.CorrectOptionIndex, null, q.Explanation))
                .ToList();
            if (questions.Count == 0)
                return existing;

            var glossary = content.Glossary
                .Where(g => !string.IsNullOrWhiteSpace(g.Word) && !string.IsNullOrWhiteSpace(g.Translation))
                .Select(g => GlossaryEntry.Create(g.Word, g.Translation, g.ExampleSentence))
                .ToList();

            var passage = existing ?? ReadingPassage.ForTopic(
                topic.Id, topic.Title, topic.Category, topic.Level, DateTimeOffset.UtcNow);
            passage.FillContent(content.Body, glossary, questions);
            await _passages.SaveAsync(passage, cancellationToken);
            return passage;
        }
        catch (DomainException)
        {
            // Generated content failed a domain invariant - keep the lesson pending.
            return existing;
        }
    }
}
