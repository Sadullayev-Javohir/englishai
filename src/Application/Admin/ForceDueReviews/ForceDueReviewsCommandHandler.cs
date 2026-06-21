using Application.Common;
using Application.Identity.Dtos;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Common;
using Domain.Vocabulary;
using MediatR;

namespace Application.Admin.ForceDueReviews;

/// <summary>
/// Authorizes the caller as an admin (403 otherwise), then pulls all of their own saved words into
/// the SRS review queue by making each due right now. Each word's learning stage / fail count is
/// preserved (mastered words are revived to the first checkpoint), so this only changes *when* the
/// next review lands - it never fabricates progress.
/// </summary>
public sealed class ForceDueReviewsCommandHandler
    : IRequestHandler<ForceDueReviewsCommand, ForceDueReviewsResultDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IVocabularyRepository _vocabulary;
    private readonly IVocabularyTopicRepository _topics;
    private readonly IVocabularyPassageGenerator _generator;
    private readonly TimeProvider _clock;

    public ForceDueReviewsCommandHandler(
        IAdminAuthorization admin,
        IVocabularyRepository vocabulary,
        IVocabularyTopicRepository topics,
        IVocabularyPassageGenerator generator,
        TimeProvider clock)
    {
        _admin = admin;
        _vocabulary = vocabulary;
        _topics = topics;
        _generator = generator;
        _clock = clock;
    }

    public async Task<ForceDueReviewsResultDto> Handle(
        ForceDueReviewsCommand request, CancellationToken cancellationToken)
    {
        var role = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (role is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var now = _clock.GetUtcNow();
        var items = (await _vocabulary.GetByLearnerIdAsync(request.RequestingUserId, cancellationToken)).ToList();
        var seededWords = 0;
        if (items.Count == 0)
        {
            seededWords = await SeedMyFamilyWordsAsync(
                request.RequestingUserId, now, items, cancellationToken);
        }

        var madeDue = seededWords;
        foreach (var item in items)
        {
            // Already due? It's in the queue - no need to rewrite an unchanged schedule.
            if (item.IsDue(now))
                continue;

            item.MakeDueForReview(now);
            await _vocabulary.SaveAsync(item, cancellationToken);
            madeDue++;
        }

        // Every non-mastered word is now due, so this is the size of the review queue the caller
        // will find waiting on the review screen.
        var dueNow = items.Count(item => item.IsDue(now));
        return new ForceDueReviewsResultDto(
            TotalWords: items.Count, MadeDue: madeDue, DueNow: dueNow, SeededWords: seededWords);
    }

    private async Task<int> SeedMyFamilyWordsAsync(
        Guid learnerId,
        DateTimeOffset now,
        List<VocabularyItem> items,
        CancellationToken cancellationToken)
    {
        var topic = (await _topics.GetByLevelAsync(CefrLevel.A1, cancellationToken))
            .FirstOrDefault(item => item.Slug == "a1-my-family");
        if (topic is null)
        {
            topic = VocabularyTopic.Curate(
                "a1-my-family",
                "My Family",
                "Mening oilam",
                "family_people",
                "to-be",
                CefrLevel.A1,
                now);
        }

        if (topic.NeedsContentRefresh)
        {
            try
            {
                var content = await _generator.GenerateAsync(
                    topic.Title, topic.Level, VocabularyTopic.TargetWordCount, cancellationToken);
                var words = content.Words
                    .Where(word => !string.IsNullOrWhiteSpace(word.Word) && !string.IsNullOrWhiteSpace(word.Translation))
                    .Select(word => TopicWord.Create(
                        word.Word, word.Translation, word.ExampleSentence, PartOfSpeechParser.Parse(word.Pos)))
                    .ToList();
                if (content.HasContent && words.Count > 0)
                {
                    topic.FillContent(content.Passage, words);
                    await _topics.SaveAsync(topic, cancellationToken);
                }
            }
            catch (DomainException)
            {
                return 0;
            }
        }

        foreach (var word in topic.Words)
        {
            var item = VocabularyItem.Learn(
                learnerId,
                word.Word,
                word.Translation,
                now,
                word.ExampleSentence,
                VocabularySource.Vocabulary,
                topic.Id,
                word.PartOfSpeech);
            item.MakeDueForReview(now);
            await _vocabulary.SaveAsync(item, cancellationToken);
            items.Add(item);
        }

        return topic.Words.Count;
    }
}
