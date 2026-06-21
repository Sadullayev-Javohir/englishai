using Domain.Assessment;
using Domain.Common;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

public class VocabularyTopicTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static VocabularyTopic Curated() =>
        VocabularyTopic.Curate("b1-saving-water", "Saving Water", "Suvni tejash", "environment", "present-perfect", CefrLevel.B1, Now);

    private static VocabularyTopic Filled()
    {
        var topic = Curated();
        var words = new[]
        {
            TopicWord.Create("reduce", "kamaytirmoq", "We must reduce how much water we waste."),
            TopicWord.Create("waste", "isrof qilmoq", "It is wrong to waste clean water."),
            TopicWord.Create("supply", "ta'minot", "The city water supply is limited in summer."),
            TopicWord.Create("drought", "qurg'oqchilik", "A long drought can dry the rivers."),
            TopicWord.Create("conserve", "asramoq", "Every family can conserve water at home."),
        };
        topic.FillContent("A short passage about saving water at home and in the city.", words);
        return topic;
    }

    [Fact]
    public void Curate_starts_pending_with_no_passage_or_words()
    {
        var topic = Curated();

        topic.Status.Should().Be(VocabularyTopicStatus.Pending);
        topic.IsFilled.Should().BeFalse();
        topic.Passage.Should().BeEmpty();
        topic.Words.Should().BeEmpty();
        topic.Slug.Should().Be("b1-saving-water");
    }

    [Fact]
    public void Curate_rejects_blank_metadata()
    {
        var act = () => VocabularyTopic.Curate(" ", "T", "U", "c", "to-be", CefrLevel.A1, Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FillContent_stores_passage_and_words_and_marks_filled()
    {
        var topic = Filled();

        topic.IsFilled.Should().BeTrue();
        topic.Passage.Should().NotBeEmpty();
        topic.Words.Should().HaveCount(5);
    }

    [Fact]
    public void FillContent_rejects_empty_passage_or_words()
    {
        var topic = Curated();

        var noPassage = () => topic.FillContent(" ", new[] { TopicWord.Create("a", "b", "a x") });
        noPassage.Should().Throw<DomainException>();

        var noWords = () => topic.FillContent("text", Array.Empty<TopicWord>());
        noWords.Should().Throw<DomainException>();
    }

    [Fact]
    public void BuildQuiz_throws_when_not_filled()
    {
        var act = () => Curated().BuildQuiz();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void BuildQuiz_blanks_the_target_word_and_includes_it_as_an_option()
    {
        var quiz = Filled().BuildQuiz();

        quiz.Should().NotBeEmpty();
        foreach (var q in quiz)
        {
            q.Prompt.Should().Contain(TopicQuizQuestion.Blank);
            q.Prompt.Should().NotContain(q.Word);
            q.Options.Should().HaveCount(4);
            q.Options.Should().Contain(q.Word);
            q.Options[q.CorrectOptionIndex].Should().Be(q.Word);
        }
    }

    [Fact]
    public void BuildQuiz_is_deterministic_across_calls()
    {
        var topic = Filled();

        var first = topic.BuildQuiz();
        var second = topic.BuildQuiz();

        first.Select(q => q.Word).Should().Equal(second.Select(q => q.Word));
        for (var i = 0; i < first.Count; i++)
            first[i].Options.Should().Equal(second[i].Options);
    }

    [Fact]
    public void GradeQuiz_scores_all_correct_answers_as_full_marks()
    {
        var topic = Filled();
        var quiz = topic.BuildQuiz();
        var answers = quiz.ToDictionary(q => q.Index, q => q.CorrectOptionIndex);

        var result = topic.GradeQuiz(answers);

        result.TotalQuestions.Should().Be(quiz.Count);
        result.CorrectCount.Should().Be(quiz.Count);
        result.ScorePercent.Should().Be(100);
    }

    [Fact]
    public void GradeQuiz_counts_unanswered_questions_as_incorrect()
    {
        var topic = Filled();

        var result = topic.GradeQuiz(new Dictionary<int, int>());

        result.CorrectCount.Should().Be(0);
        result.ScorePercent.Should().Be(0);
        result.Outcomes.Should().OnlyContain(o => !o.IsCorrect);
    }

    [Fact]
    public void Curated_topic_needs_content_refresh()
    {
        var topic = Curated();

        topic.ContentVersion.Should().Be(0);
        topic.NeedsContentRefresh.Should().BeTrue();
    }

    [Fact]
    public void FillContent_stamps_current_version_and_clears_refresh_flag()
    {
        var topic = Filled();

        topic.ContentVersion.Should().Be(VocabularyTopic.CurrentContentVersion);
        topic.NeedsContentRefresh.Should().BeFalse();
    }

    [Fact]
    public void FillContent_preserves_part_of_speech()
    {
        var topic = Curated();
        topic.FillContent("She runs quickly to the busy market.", new[]
        {
            TopicWord.Create("run", "yugurmoq", "She runs quickly to the busy market.", PartOfSpeech.Verb),
            TopicWord.Create("market", "bozor", "She runs quickly to the busy market.", PartOfSpeech.Noun),
        });

        topic.Words.Single(w => w.Word == "run").PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        topic.Words.Single(w => w.Word == "market").PartOfSpeech.Should().Be(PartOfSpeech.Noun);
    }

    [Fact]
    public void BuildQuiz_prefers_distractors_of_the_same_part_of_speech()
    {
        var topic = Curated();
        // One verb under test, three other verbs (ideal distractors), and four nouns as filler.
        topic.FillContent(
            "We plan to cook, clean, paint and travel; the plan, food, room and trip all matter.",
            new[]
            {
                TopicWord.Create("plan", "rejalashtirmoq", "We plan to cook food today.", PartOfSpeech.Verb),
                TopicWord.Create("cook", "pishirmoq", "We plan to cook food today.", PartOfSpeech.Verb),
                TopicWord.Create("clean", "tozalamoq", "We clean the room often.", PartOfSpeech.Verb),
                TopicWord.Create("paint", "bo'yamoq", "We paint the wall white.", PartOfSpeech.Verb),
                TopicWord.Create("food", "ovqat", "We plan to cook food today.", PartOfSpeech.Noun),
                TopicWord.Create("room", "xona", "We clean the room often.", PartOfSpeech.Noun),
                TopicWord.Create("trip", "sayohat", "The trip was long and fun.", PartOfSpeech.Noun),
                TopicWord.Create("wall", "devor", "We paint the wall white.", PartOfSpeech.Noun),
            });

        var verbs = new[] { "plan", "cook", "clean", "paint" };
        var quiz = topic.BuildQuiz();
        var verbQuestion = quiz.First(q => verbs.Contains(q.Word, StringComparer.OrdinalIgnoreCase));

        // With three other verbs available, all three distractors must be verbs (no noun leaks in).
        verbQuestion.Options.Should().OnlyContain(o => verbs.Contains(o, StringComparer.OrdinalIgnoreCase));
    }
}
