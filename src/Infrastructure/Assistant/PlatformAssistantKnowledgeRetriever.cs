using System.Text;
using Application.Assistant.Ports;
using Application.Books.Ports;
using Application.Grammar.Ports;
using Application.Learning.Ports;
using Application.Listening.Ports;
using Application.Reading.Ports;
using Application.Subscription.Access;
using Application.Video.Ports;
using Application.Vocabulary.Ports;
using Application.Writing.Ports;
using Domain.Learning;
using Domain.Vocabulary;

namespace Infrastructure.Assistant;

public sealed class PlatformAssistantKnowledgeRetriever(
    IVocabularyTopicRepository vocabulary,
    IGrammarRepository grammar,
    IReadingRepository reading,
    IWritingTaskRepository writing,
    IListeningRepository listening,
    IBookRepository books,
    IBookProgressStore bookProgress,
    IVideoRepository videos,
    ITopicAccessPolicy topicAccess,
    ILearnerProfileRepository profiles,
    ITopicCompletionStore completions) : IAssistantKnowledgeRetriever
{
    private const int MaxSources = 5;
    private const int MaxContextLength = 12000;

    public async Task<AssistantKnowledgeResult> RetrieveAsync(
        AssistantKnowledgeRequest request,
        CancellationToken cancellationToken = default)
    {
        var terms = Terms(request.Question, request.FocusText);
        var candidates = new List<AssistantKnowledgeSource>();
        var topics = await vocabulary.GetAllAsync(cancellationToken);
        var access = new Dictionary<Guid, bool>();
        foreach (var topic in topics)
            access[topic.Id] = (await topicAccess.EvaluateAsync(request.LearnerId, topic.Id, cancellationToken)).IsAllowed;

        AddVocabulary(candidates, topics.Where(topic => access[topic.Id]), request, terms);
        await AddGrammarAsync(candidates, access, request, terms, cancellationToken);
        await AddReadingAsync(candidates, access, request, terms, cancellationToken);
        await AddWritingAsync(candidates, access, request, terms, cancellationToken);
        await AddListeningAsync(candidates, access, request, terms, cancellationToken);
        await AddBooksAsync(candidates, request, terms, cancellationToken);
        await AddVideoAsync(candidates, request, terms, cancellationToken);
        await AddProgressAsync(candidates, request, terms, cancellationToken);

        var selected = candidates
            .Where(x => x.Relevance > 0)
            .OrderByDescending(x => x.Relevance)
            .ThenBy(x => x.Title)
            .GroupBy(x => $"{x.Area}:{x.ResourceId}")
            .Select(group => group.First())
            .Take(MaxSources)
            .ToArray();
        if (selected.Length == 0) return AssistantKnowledgeResult.Empty;

        var context = new StringBuilder("PLATFORM KNOWLEDGE (authorized for this learner):\n");
        foreach (var source in selected)
        {
            var block = $"\n[SOURCE area={source.Area} title={source.Title} route={source.Route}]\n{source.Content}\n";
            if (context.Length + block.Length > MaxContextLength) break;
            context.Append(block);
        }
        return new AssistantKnowledgeResult(context.ToString().Trim(), selected);
    }

    private static void AddVocabulary(
        ICollection<AssistantKnowledgeSource> output,
        IEnumerable<VocabularyTopic> topics,
        AssistantKnowledgeRequest request,
        IReadOnlySet<string> terms)
    {
        foreach (var topic in topics)
        {
            var topicText = $"{topic.Title} {topic.TitleUz} {topic.Category} {topic.Passage}";
            var matchingWords = topic.Words
                .Select(word => new { Word = word, Score = Score(word.Word, terms, exactBoost: 90) + Score($"{word.Translation} {word.ExampleSentence} {word.UsageNote}", terms) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(8)
                .ToArray();
            var score = BaseScore(request, "vocabulary", topic.Id.ToString(), topic.Title)
                        + Score(topicText, terms)
                        + matchingWords.Sum(x => x.Score);
            if (score <= 0) continue;
            var words = matchingWords.Length > 0 ? matchingWords.Select(x => x.Word) : topic.Words.Take(12);
            var content = new StringBuilder()
                .Append("Topic: ").Append(topic.Title).Append(" — ").AppendLine(topic.TitleUz)
                .Append("Level: ").AppendLine(topic.Level.ToString())
                .Append("Passage: ").AppendLine(Limit(topic.Passage, 1800))
                .AppendLine("Words:");
            foreach (var word in words)
                content.Append("- ").Append(word.Word).Append(" | ").Append(word.Translation)
                    .Append(" | part of speech: ").Append(word.PartOfSpeech)
                    .Append(" | example: ").Append(word.ExampleSentence ?? "not provided")
                    .Append(" | usage: ").AppendLine(word.UsageNote ?? "not provided");
            output.Add(Source("vocabulary", "topic", topic.Id, topic.Title, $"/app/vocabulary/topic/{topic.Id}", content.ToString(), score));
        }
    }

    private async Task AddGrammarAsync(ICollection<AssistantKnowledgeSource> output, IReadOnlyDictionary<Guid, bool> access,
        AssistantKnowledgeRequest request, IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        foreach (var lesson in await grammar.GetAllAsync(cancellationToken))
        {
            if (lesson.VocabularyTopicId is Guid topicId && (!access.TryGetValue(topicId, out var allowed) || !allowed)) continue;
            var text = $"{lesson.Topic} {lesson.ContextIntro} {lesson.Explanation} {lesson.CuratedSummaryUz} {string.Join(' ', lesson.CuratedFormulas)}";
            var score = BaseScore(request, "grammar", (lesson.VocabularyTopicId ?? lesson.Id).ToString(), lesson.Topic) + Score(text, terms);
            if (score <= 0) continue;
            var resourceId = lesson.VocabularyTopicId ?? lesson.Id;
            var content = $"Topic: {lesson.Topic}\nLevel: {lesson.Level}\nIntroduction: {lesson.ContextIntro}\nExplanation: {lesson.Explanation}\nSummary: {lesson.CuratedSummaryUz}\nFormulas: {string.Join(" | ", lesson.CuratedFormulas)}\nRules: {string.Join(" | ", lesson.CuratedRules.Select(rule => $"{rule.HeadingUz}: {rule.BodyUz}"))}";
            output.Add(Source("grammar", "topic", resourceId, lesson.Topic, $"/app/grammar/topic/{resourceId}", Limit(content, 3000), score));
        }
    }

    private async Task AddReadingAsync(ICollection<AssistantKnowledgeSource> output, IReadOnlyDictionary<Guid, bool> access,
        AssistantKnowledgeRequest request, IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        foreach (var passage in await reading.GetAllAsync(cancellationToken))
        {
            if (passage.VocabularyTopicId is Guid topicId && (!access.TryGetValue(topicId, out var allowed) || !allowed)) continue;
            var text = $"{passage.Title} {passage.Topic} {passage.Body} {string.Join(' ', passage.Glossary.Select(x => $"{x.Word} {x.Translation}"))}";
            var score = BaseScore(request, "reading", (passage.VocabularyTopicId ?? passage.Id).ToString(), passage.Title) + Score(text, terms);
            if (score <= 0) continue;
            var resourceId = passage.VocabularyTopicId ?? passage.Id;
            var content = $"Title: {passage.Title}\nTopic: {passage.Topic}\nLevel: {passage.Level}\nPassage: {Limit(passage.Body, 2600)}\nGlossary: {string.Join(" | ", passage.Glossary.Select(x => $"{x.Word} — {x.Translation}"))}";
            output.Add(Source("reading", "topic", resourceId, passage.Title, $"/reading/topic/{resourceId}", content, score));
        }
    }

    private async Task AddWritingAsync(ICollection<AssistantKnowledgeSource> output, IReadOnlyDictionary<Guid, bool> access,
        AssistantKnowledgeRequest request, IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        foreach (var task in await writing.GetAllAsync(cancellationToken))
        {
            if (task.VocabularyTopicId is Guid topicId && (!access.TryGetValue(topicId, out var allowed) || !allowed)) continue;
            var text = $"{task.Prompt} {string.Join(' ', task.Guidance)}";
            var score = BaseScore(request, "writing", (task.VocabularyTopicId ?? task.Id).ToString(), task.Prompt) + Score(text, terms);
            if (score <= 0) continue;
            var resourceId = task.VocabularyTopicId ?? task.Id;
            var content = $"Prompt: {task.Prompt}\nWord target: {task.MinWords}-{task.MaxWords}\nGuidance: {string.Join(" | ", task.Guidance)}";
            output.Add(Source("writing", "topic", resourceId, Limit(task.Prompt, 100), $"/writing/topic/{resourceId}", content, score));
        }
    }

    private async Task AddListeningAsync(ICollection<AssistantKnowledgeSource> output, IReadOnlyDictionary<Guid, bool> access,
        AssistantKnowledgeRequest request, IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        foreach (var exercise in await listening.GetAllOrderedByLevelAsync(cancellationToken))
        {
            if (exercise.VocabularyTopicId is Guid topicId && (!access.TryGetValue(topicId, out var allowed) || !allowed)) continue;
            var text = $"{exercise.Title} {exercise.Topic} {exercise.Transcript}";
            var score = BaseScore(request, "listening", (exercise.VocabularyTopicId ?? exercise.Id).ToString(), exercise.Title) + Score(text, terms);
            if (score <= 0) continue;
            var resourceId = exercise.VocabularyTopicId ?? exercise.Id;
            var content = $"Title: {exercise.Title}\nTopic: {exercise.Topic}\nLevel: {exercise.Level}\nTranscript: {Limit(exercise.Transcript, 2800)}";
            output.Add(Source("listening", "topic", resourceId, exercise.Title, $"/listening/topic/{resourceId}", content, score));
        }
    }

    private async Task AddBooksAsync(ICollection<AssistantKnowledgeSource> output, AssistantKnowledgeRequest request,
        IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        var progressByBook = (await bookProgress.GetByLearnerAsync(request.LearnerId, cancellationToken)).ToDictionary(x => x.BookId);
        foreach (var book in await books.GetAllAsync(cancellationToken))
        {
            progressByBook.TryGetValue(book.Id, out var progress);
            var accessibleSections = book.Sections.Where(section =>
                section.Order == 1 || (progress?.HasPassed(book.Sections.ElementAt(section.Order - 2).Id) ?? false)).ToArray();
            var text = $"{book.Title} {book.TitleUz} {book.Author} {book.Topic} {book.Synopsis} {string.Join(' ', accessibleSections.Select(x => $"{x.Title} {x.Body}"))}";
            var score = BaseScore(request, "books", book.Id.ToString(), book.Title) + Score(text, terms);
            if (score <= 0) continue;
            var content = $"Book: {book.Title} — {book.TitleUz}\nAuthor: {book.Author}\nTopic: {book.Topic}\nLevel: {book.Level}\nSynopsis: {book.Synopsis}\nAccessible sections: {string.Join("\n", accessibleSections.Where(x => x.IsFilled).Take(2).Select(x => $"{x.Title}: {Limit(x.Body, 1800)}"))}";
            output.Add(Source("books", "book", book.Id, book.Title, $"/books/{book.Id}", Limit(content, 3800), score));
        }
    }

    private async Task AddVideoAsync(ICollection<AssistantKnowledgeSource> output, AssistantKnowledgeRequest request,
        IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        if (request.Area != "video" || !Guid.TryParse(request.ResourceId, out var videoId)) return;
        var lesson = await videos.GetByIdAsync(videoId, cancellationToken);
        if (lesson is null) return;
        var text = $"{lesson.Title} {lesson.Topic} {string.Join(' ', lesson.Transcript.Select(x => x.EnglishText))} {string.Join(' ', lesson.Glossary.Select(x => $"{x.Word} {x.UzbekMeaning}"))}";
        var score = 180 + Score(text, terms);
        var content = $"Title: {lesson.Title}\nTopic: {lesson.Topic}\nLevel: {lesson.Level}\nTranscript: {Limit(string.Join(' ', lesson.Transcript.Select(x => x.EnglishText)), 3500)}\nGlossary: {string.Join(" | ", lesson.Glossary.Select(x => $"{x.Word} — {x.UzbekMeaning}"))}";
        output.Add(Source("video", "video", lesson.Id, lesson.Title, $"/video/{lesson.Id}/play", content, score));
    }

    private async Task AddProgressAsync(ICollection<AssistantKnowledgeSource> output, AssistantKnowledgeRequest request,
        IReadOnlySet<string> terms, CancellationToken cancellationToken)
    {
        if (request.Area != "progress" && !terms.Overlaps(new[] { "progress", "daraja", "level", "keyingi", "tavsiya", "zaif", "natija" })) return;
        var profile = await profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var records = await completions.GetByLearnerAsync(request.LearnerId, cancellationToken);
        var moduleScores = records.SelectMany(record => new[]
        {
            (Name: "Vocabulary", Score: record.ScoreFor(SkillType.Vocabulary)),
            (Name: "Grammar", Score: record.ScoreFor(SkillType.Grammar)),
            (Name: "Listening", Score: record.ScoreFor(SkillType.Listening)),
            (Name: "Reading", Score: record.ScoreFor(SkillType.Reading)),
            (Name: "Writing", Score: record.ScoreFor(SkillType.Writing)),
            (Name: "Speaking", Score: record.ScoreFor(SkillType.Speaking)),
        }).GroupBy(x => x.Name).Select(group => (group.Key, Average: group.Average(x => x.Score))).OrderBy(x => x.Average).ToArray();
        var content = $"Learner level: {profile?.OverallLevel.ToString() ?? "unknown"}\nStarted topics: {records.Count}\nMastered topics: {records.Count(x => x.IsMastered)}\nSkill averages: {string.Join(" | ", moduleScores.Select(x => $"{x.Key}: {x.Average:0}%"))}\nWeakest skill: {moduleScores.FirstOrDefault().Key ?? "not enough data"}. Recommend the next practice based on these facts without inventing progress.";
        output.Add(new AssistantKnowledgeSource("progress", "page", request.LearnerId.ToString(), "Shaxsiy progress", "/progress", content, 160));
    }

    private static AssistantKnowledgeSource Source(string area, string type, Guid id, string title, string route, string content, double score) =>
        new(area, type, id.ToString(), title, route, content, score);

    private static double BaseScore(AssistantKnowledgeRequest request, string area, string resourceId, string title)
    {
        var score = string.Equals(request.Area, area, StringComparison.OrdinalIgnoreCase) ? 25 : 0;
        if (!string.IsNullOrWhiteSpace(request.ResourceId) && string.Equals(request.ResourceId, resourceId, StringComparison.OrdinalIgnoreCase)) score += 180;
        if (!string.IsNullOrWhiteSpace(request.Title) && title.Contains(request.Title, StringComparison.OrdinalIgnoreCase)) score += 35;
        return score;
    }

    private static double Score(string? text, IReadOnlySet<string> terms, double exactBoost = 0)
    {
        if (string.IsNullOrWhiteSpace(text) || terms.Count == 0) return 0;
        var normalized = Normalize(text);
        var score = 0d;
        foreach (var term in terms)
        {
            if (string.Equals(normalized, term, StringComparison.OrdinalIgnoreCase)) score += exactBoost;
            else if (normalized.Split(' ').Contains(term)) score += 18;
            else if (normalized.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 5;
        }
        return score;
    }

    private static HashSet<string> Terms(params string[] values) => values
        .SelectMany(value => Normalize(value).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Where(term => term.Length > 2 && !StopWords.Contains(term))
        .Take(24)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string? value) => new((value ?? string.Empty).ToLowerInvariant()
        .Select(character => char.IsLetterOrDigit(character) ? character : ' ').ToArray());

    private static string Limit(string? value, int max) => string.IsNullOrWhiteSpace(value)
        ? string.Empty
        : value.Trim()[..Math.Min(value.Trim().Length, max)];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "what", "does", "this", "that", "with", "from", "have", "about", "please", "word", "mean",
        "nima", "qanday", "haqida", "uchun", "bilan", "qayerda", "ishlatiladi", "tushuntir", "bering"
    };
}
