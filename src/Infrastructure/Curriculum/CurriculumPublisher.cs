using System.Text.Json;
using Application.Grammar.Content;
using Application.Grammar.Models;
using Domain.Books;
using Domain.Curriculum;
using Domain.Grammar;
using Domain.Learning;
using Domain.Listening;
using Domain.Reading;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Curriculum;

public sealed class CurriculumPublisher
{
    private readonly EnglishAiDbContext _db;
    public CurriculumPublisher(EnglishAiDbContext db) => _db = db;

    public async Task PublishAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _db.CurriculumGenerationRuns.FirstAsync(x => x.Id == runId, cancellationToken);
        var items = await _db.CurriculumGenerationItems.Where(x => x.RunId == runId).ToListAsync(cancellationToken);
        if (items.Count == 0 || items.Any(x => x.Status != CurriculumItemStatus.Approved))
            throw new InvalidOperationException("Every curriculum item must be approved before publish.");
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var group in items.Where(x => x.TopicId != null).GroupBy(x => x.TopicId!.Value))
            await PublishTopicAsync(group.Key, group.ToDictionary(x => x.Module), cancellationToken);
        foreach (var item in items.Where(x => x.Module == "books")) await PublishBookAsync(item, cancellationToken);
        foreach (var item in items) item.Publish(DateTimeOffset.UtcNow);
        run.Publish(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
    }

    private async Task PublishTopicAsync(Guid topicId, IReadOnlyDictionary<string, CurriculumGenerationItem> items,
        CancellationToken cancellationToken)
    {
        var topic = await _db.VocabularyTopics.FirstAsync(x => x.Id == topicId, cancellationToken);
        using var vocabDoc = JsonDocument.Parse(items["vocabulary"].PayloadJson!);
        var words = vocabDoc.RootElement.GetProperty("words").EnumerateArray().Select(w =>
        {
            var pos = PartOfSpeechParser.Parse(S(w, "pos"));
            return TopicWord.Create(S(w, "w")!, S(w, "uz")!, S(w, "ex"), pos,
                LexicalCategoryParser.Parse(S(w, "category"), pos), S(w, "register"), S(w, "usageNote"));
        }).ToArray();
        topic.UpdateContent(S(vocabDoc.RootElement, "passage")!, words);

        using var grammarDoc = JsonDocument.Parse(items["grammar"].PayloadJson!);
        var grammar = await _db.GrammarLessons.FirstOrDefaultAsync(x => x.VocabularyTopicId == topicId, cancellationToken)
            ?? GrammarLesson.ForTopic(topicId, topic.Title, GrammarFocusCategory.For(topic.GrammarFocusCode), topic.Level,
                topic.GrammarFocusCode, DateTimeOffset.UtcNow);
        grammar.FillContent(SAny(grammarDoc.RootElement, "intro", "contextIntro", "introduction")!,
            SAny(grammarDoc.RootElement, "rule", "explanation", "grammarRule")!,
            grammarDoc.RootElement.GetProperty("examples").EnumerateArray().Select(x => GrammarExample.Create(S(x, "en")!, S(x, "uz"))),
            Array.Empty<GrammarCommonMistake>(),
            GetAny(grammarDoc.RootElement, "exercises", "questions").EnumerateArray().Select(ToGrammarExercise),
            grammarDoc.RootElement.GetProperty("tasks").EnumerateArray().Select(ToGrammarTask));
        var curated = CuratedGrammarLessonCatalog.TryGet(topic.GrammarFocusCode);
        if (curated is not null)
            grammar.ReplaceCuratedRuleContent(curated.TitleUz, curated.SummaryUz, curated.Formulas,
                curated.Rules.Select(x => GrammarCuratedRule.Create(x.HeadingUz, x.BodyUz)));
        if (_db.Entry(grammar).State == EntityState.Detached) _db.GrammarLessons.Add(grammar);

        using var readingDoc = JsonDocument.Parse(items["reading"].PayloadJson!);
        var reading = await _db.ReadingPassages.FirstOrDefaultAsync(x => x.VocabularyTopicId == topicId, cancellationToken)
            ?? ReadingPassage.ForTopic(topicId, topic.Title, topic.Category, topic.Level, DateTimeOffset.UtcNow);
        reading.FillContent(S(readingDoc.RootElement, "body")!,
            readingDoc.RootElement.GetProperty("glossary").EnumerateArray().Select(x => GlossaryEntry.Create(S(x, "w")!, S(x, "uz")!, S(x, "ex"))),
            readingDoc.RootElement.GetProperty("questions").EnumerateArray().Select(ToReadingQuestion));
        if (_db.Entry(reading).State == EntityState.Detached) _db.ReadingPassages.Add(reading);

        using var listeningDoc = JsonDocument.Parse(items["listening"].PayloadJson!);
        var listening = await _db.ListeningExercises.FirstOrDefaultAsync(x => x.VocabularyTopicId == topicId, cancellationToken)
            ?? ListeningExercise.ForTopic(topicId, topic.Title, topic.Category, topic.Level, DateTimeOffset.UtcNow);
        listening.FillContent(S(listeningDoc.RootElement, "transcript")!,
            listeningDoc.RootElement.GetProperty("questions").EnumerateArray().Select(ToListeningQuestion));
        if (_db.Entry(listening).State == EntityState.Detached) _db.ListeningExercises.Add(listening);

        using var writingDoc = JsonDocument.Parse(items["writing"].PayloadJson!);
        var writing = await _db.WritingTasks.FirstOrDefaultAsync(x => x.VocabularyTopicId == topicId, cancellationToken);
        if (writing is null)
        {
            var range = topic.Level switch { Domain.Assessment.CefrLevel.A1 => (40,70), Domain.Assessment.CefrLevel.A2 => (50,80), Domain.Assessment.CefrLevel.B1 => (120,200), Domain.Assessment.CefrLevel.B2 => (150,250), Domain.Assessment.CefrLevel.C1 => (200,300), _ => (250,350) };
            writing = Domain.Writing.WritingTask.ForTopic(topicId, topic.Level, range.Item1, range.Item2, DateTimeOffset.UtcNow); _db.WritingTasks.Add(writing);
        }
        writing.FillContent(S(writingDoc.RootElement, "prompt")!, writingDoc.RootElement.GetProperty("guidance").EnumerateArray().Select(x => x.GetString()!));

        using var speakingDoc = JsonDocument.Parse(items["speaking"].PayloadJson!);
        var oldBlueprint = await _db.TopicSpeakingBlueprints.FirstOrDefaultAsync(x => x.TopicId == topicId, cancellationToken);
        if (oldBlueprint is not null) _db.TopicSpeakingBlueprints.Remove(oldBlueprint);
        var levelTopics = await _db.VocabularyTopics.Where(x => x.Level == topic.Level).ToListAsync(cancellationToken);
        _db.TopicSpeakingBlueprints.Add(TopicSpeakingBlueprint.Create(topicId, topic.Level,
            S(speakingDoc.RootElement, "objective")!, topic.GrammarFocusCode, CurriculumPrompts.ReviewFocus(topic, levelTopics),
            speakingDoc.RootElement.GetProperty("priorityWords").GetRawText(), speakingDoc.RootElement.GetProperty("questions").GetRawText(),
            speakingDoc.RootElement.GetProperty("rubric").GetRawText(), 4, DateTimeOffset.UtcNow));
    }

    private async Task PublishBookAsync(CurriculumGenerationItem item, CancellationToken cancellationToken)
    {
        var book = await _db.Books.FirstAsync(x => x.Id == item.BookId, cancellationToken);
        var section = book.FindSection(item.SectionId!.Value)!;
        using var doc = JsonDocument.Parse(item.PayloadJson!);
        section.FillContent(S(doc.RootElement, "body")!, doc.RootElement.GetProperty("questions").EnumerateArray().Select(ToBookQuestion));
    }

    private static GrammarExercise ToGrammarExercise(JsonElement x) => GrammarExercise.Create(
        S(x, "type")?.ToLowerInvariant() switch { "fill" or "fillinblank" => GrammarExerciseType.FillInBlank, "rephrase" => GrammarExerciseType.Rephrase, _ => GrammarExerciseType.Recognition },
        SAny(x, "q", "prompt", "question")!, Options(x), I(x, "answer"), null, S(x, "why"));
    private static GrammarApplicationTask ToGrammarTask(JsonElement x) => GrammarApplicationTask.Create(
        S(x, "skill")?.Equals("writing", StringComparison.OrdinalIgnoreCase) == true ? SkillType.Writing : SkillType.Speaking, S(x, "prompt")!);
    private static ReadingQuestion ToReadingQuestion(JsonElement x) => ReadingQuestion.Create(S(x, "q")!, Options(x), I(x, "answer"), null, S(x, "why"));
    private static ListeningQuestion ToListeningQuestion(JsonElement x) => ListeningQuestion.Create(S(x, "q")!, Options(x), I(x, "answer"), null, S(x, "why"));
    private static BookQuestion ToBookQuestion(JsonElement x) => BookQuestion.Create(S(x, "q")!, Options(x), I(x, "answer"), S(x, "why"));
    private static IReadOnlyList<string> Options(JsonElement x) => x.GetProperty("options").EnumerateArray().Select(o => o.GetString()!).ToArray();
    private static string? S(JsonElement x, string name) => x.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string? SAny(JsonElement x, params string[] names) => names.Select(name => S(x, name)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    private static JsonElement GetAny(JsonElement x, params string[] names) => names.Select(name => x.TryGetProperty(name, out var value) ? value : default).First(value => value.ValueKind != JsonValueKind.Undefined);
    private static int I(JsonElement x, string name) => x.GetProperty(name).GetInt32();
}
