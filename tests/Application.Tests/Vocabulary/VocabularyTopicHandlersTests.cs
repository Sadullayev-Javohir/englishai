using Application.Gamification;
using Application.Common;
using Application.Learning.Ports;
using Application.Tests.Common;
using Application.Tests.Learning;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Vocabulary.CheckWordPronunciation;
using Application.Vocabulary.GetVocabularyTopic;
using Application.Vocabulary.GetVocabularyTopics;
using Application.Vocabulary.Models;
using Application.Vocabulary.Ports;
using Application.Vocabulary.SubmitTopicQuiz;
using Application.Vocabulary;
using Domain.Assessment;
using Domain.Speaking;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vocabulary;

public class VocabularyTopicHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IVocabularyPassageGenerator _generator = Substitute.For<IVocabularyPassageGenerator>();
    private readonly IPhonemeVisualLibrary _phonetics = Substitute.For<IPhonemeVisualLibrary>();

    private static VocabularyTopic PendingTopic() =>
        VocabularyTopic.Curate("b1-saving-water", "Saving Water", "Suvni tejash", "environment", "present-perfect", CefrLevel.B1, Now);

    private static VocabularyTopic LevelTopic(CefrLevel level) =>
        VocabularyTopic.Curate($"{level}-topic", $"Topic {level}", "Mavzu", "general", "present-perfect", level, Now);

    private static VocabularyTopic FilledTopic()
    {
        var topic = PendingTopic();
        topic.FillContent("Passage about water.", new[]
        {
            TopicWord.Create("reduce", "kamaytirmoq", "We must reduce our use of water."),
            TopicWord.Create("waste", "isrof qilmoq", "Do not waste clean water."),
            TopicWord.Create("supply", "ta'minot", "The water supply is small."),
            TopicWord.Create("conserve", "asramoq", "We conserve water every day."),
        });
        return topic;
    }

    private static GeneratedTopicContent SampleContent() => new(
        "We must reduce our use of water. Do not waste clean water. The water supply is small. We conserve water every day.",
        new[]
        {
            new GeneratedTopicWord("reduce", "kamaytirmoq", "We must reduce our use of water."),
            new GeneratedTopicWord("waste", "isrof qilmoq", "Do not waste clean water."),
            new GeneratedTopicWord("supply", "ta'minot", "The water supply is small."),
            new GeneratedTopicWord("conserve", "asramoq", "We conserve water every day."),
        });

    [Fact]
    public async Task GetTopic_fills_pending_topic_enriches_ipa_and_caches()
    {
        var topic = PendingTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _generator.GenerateAsync("Saving Water", CefrLevel.B1, VocabularyTopic.TargetWordCount, Arg.Any<CancellationToken>())
            .Returns(SampleContent());
        _phonetics.GetWordPhonetics(Arg.Any<string>())
            .Returns(ci => new WordPhonetics((string)ci[0], $"/{(string)ci[0]}/", new[] { "x" }));

        var handler = new GetVocabularyTopicQueryHandler(
            _topics, _generator, _phonetics, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());
        var dto = await handler.Handle(new GetVocabularyTopicQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        dto.Passage.Should().NotBeEmpty();
        dto.Words.Should().HaveCount(4);
        dto.Words.Should().OnlyContain(w => w.Ipa != null && w.Ipa.StartsWith("/"));
        dto.Quiz.Should().NotBeEmpty();
        await _topics.Received(1).SaveAsync(topic, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTopic_stays_pending_when_generation_unavailable()
    {
        var topic = PendingTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _generator.GenerateAsync(Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(GeneratedTopicContent.Empty);

        var handler = new GetVocabularyTopicQueryHandler(
            _topics, _generator, _phonetics, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());
        var dto = await handler.Handle(new GetVocabularyTopicQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeFalse();
        dto.Passage.Should().BeEmpty();
        dto.Words.Should().BeEmpty();
        await _topics.DidNotReceive().SaveAsync(Arg.Any<VocabularyTopic>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("turn on", "yoqmoq", "turn")]
    [InlineData("a bottle of water", "suv idishi", "water")]
    [InlineData("apple", "olma", "apple")]
    public void Word_image_query_uses_the_semantic_head(
        string word, string translation, string expected)
    {
        WordImageQuery.From(word, translation).Should().Be(expected);
    }

    [Theory]
    [InlineData("run", PartOfSpeech.Verb, "person run action")]
    [InlineData("turn on", PartOfSpeech.PhrasalVerb, "person turn action")]
    [InlineData("quickly", PartOfSpeech.Adverb, "person action quickly")]
    public void Word_image_query_makes_actions_visible(
        string word, PartOfSpeech partOfSpeech, string expected)
    {
        WordImageQuery.From(word, "tarjima", partOfSpeech).Should().Be(expected);
    }

    [Fact]
    public async Task GetTopic_returns_the_persisted_local_word_image_endpoint()
    {
        var topic = FilledTopic();
        topic.Words[0].SetImage("local:stored", "Wikimedia Commons", "CC BY");
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _phonetics.GetWordPhonetics(Arg.Any<string>()).Returns((WordPhonetics?)null);
        var handler = new GetVocabularyTopicQueryHandler(
            _topics, _generator, _phonetics, TopicAccessTestDoubles.Anonymous(),
            TopicAccessTestDoubles.AllowAll());
        var dto = await handler.Handle(new GetVocabularyTopicQuery(topic.Id), CancellationToken.None);

        dto.Words[0].ImageUrl.Should().Be(
            $"/api/images/vocabulary-topics/{topic.Id}/words/{WordImageQuery.ImageId(topic.Id, topic.Words[0].Word)}");
        dto.Words[0].ImageAttribution.Should().Be("CC BY");
        dto.Words.Skip(1).Should().OnlyContain(word => word.ImageUrl == null);
        await _topics.DidNotReceive().SaveAsync(topic, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTopic_does_not_regenerate_an_already_filled_topic()
    {
        var topic = FilledTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _phonetics.GetWordPhonetics(Arg.Any<string>()).Returns((WordPhonetics?)null);

        var handler = new GetVocabularyTopicQueryHandler(
            _topics, _generator, _phonetics, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());
        var dto = await handler.Handle(new GetVocabularyTopicQuery(topic.Id), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        await _generator.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTopic_throws_when_topic_missing()
    {
        _topics.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyTopic?)null);
        var handler = new GetVocabularyTopicQueryHandler(
            _topics, _generator, _phonetics, TopicAccessTestDoubles.Anonymous(), TopicAccessTestDoubles.AllowAll());

        var act = () => handler.Handle(new GetVocabularyTopicQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTopics_uses_explicit_level_when_provided()
    {
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var speakingProgress = Substitute.For<ITopicSpeakingProgressStore>();
        speakingProgress.GetLearnedTopicIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        _topics.GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns(new[] { PendingTopic() });
        var handler = new GetVocabularyTopicsQueryHandler(
            _topics, profiles, speakingProgress, EmptyCompletions(), Substitute.For<IComplimentaryAccess>(),
            TopicAccessTestDoubles.AllowAll());

        await handler.Handle(new GetVocabularyTopicsQuery(Guid.NewGuid(), CefrLevel.C1), CancellationToken.None);

        await _topics.Received(1).GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>());
        await profiles.DidNotReceive().GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTopics_with_all_levels_returns_every_band_easiest_first()
    {
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var speakingProgress = Substitute.For<ITopicSpeakingProgressStore>();
        speakingProgress.GetLearnedTopicIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        var levels = new[]
        {
            CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2,
        };
        foreach (var lvl in levels)
            _topics.GetByLevelAsync(lvl, Arg.Any<CancellationToken>())
                .Returns(new[] { LevelTopic(lvl) });

        var handler = new GetVocabularyTopicsQueryHandler(
            _topics, profiles, speakingProgress, EmptyCompletions(), Substitute.For<IComplimentaryAccess>(),
            TopicAccessTestDoubles.AllowAll());

        var result = await handler.Handle(
            new GetVocabularyTopicsQuery(Guid.NewGuid(), Level: null, AllLevels: true), CancellationToken.None);

        result.Select(t => t.Level).Should().ContainInOrder(levels);
        // The whole catalog comes back regardless of the learner's own level - no profile lookup.
        await profiles.DidNotReceive().GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTopics_flags_topics_the_learner_has_learned_by_speaking()
    {
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var speakingProgress = Substitute.For<ITopicSpeakingProgressStore>();
        var learned = PendingTopic();
        var notLearned = PendingTopic();
        _topics.GetByLevelAsync(CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns(new[] { learned, notLearned });
        speakingProgress.GetLearnedTopicIdsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[] { learned.Id });
        var handler = new GetVocabularyTopicsQueryHandler(
            _topics, profiles, speakingProgress, EmptyCompletions(), Substitute.For<IComplimentaryAccess>(),
            TopicAccessTestDoubles.AllowAll());

        var result = await handler.Handle(
            new GetVocabularyTopicsQuery(Guid.NewGuid(), CefrLevel.C1), CancellationToken.None);

        result.Single(t => t.Id == learned.Id).Learned.Should().BeTrue();
        result.Single(t => t.Id == notLearned.Id).Learned.Should().BeFalse();
    }

    /// <summary>A completion store with no records - every topic defaults to unlocked-at-start.</summary>
    private static ITopicCompletionStore EmptyCompletions()
    {
        var completions = Substitute.For<ITopicCompletionStore>();
        completions.GetByLearnerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Vocabulary.TopicCompletionRecord>());
        return completions;
    }

    [Fact]
    public async Task SubmitQuiz_grades_against_the_stored_topic()
    {
        var topic = FilledTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var quiz = topic.BuildQuiz();
        var answers = quiz.ToDictionary(q => q.Index, q => q.CorrectOptionIndex);

        var handler = new SubmitTopicQuizCommandHandler(
            _topics, Substitute.For<ITopicCompletionStore>(), Enrollment(EmptyVocabulary()),
            Substitute.For<IDailyProgressRecorder>(), TopicAccessTestDoubles.AllowAll(),
            new FixedTimeProvider(Now));
        var result = await handler.Handle(new SubmitTopicQuizCommand(topic.Id, answers), CancellationToken.None);

        result.CorrectCount.Should().Be(quiz.Count);
        result.ScorePercent.Should().Be(100);
        result.Completion.Should().BeNull();
    }

    [Fact]
    public async Task SubmitQuiz_records_the_vocabulary_module_score_when_a_learner_is_given()
    {
        var topic = FilledTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var quiz = topic.BuildQuiz();
        var answers = quiz.ToDictionary(q => q.Index, q => q.CorrectOptionIndex);
        var completions = Substitute.For<ITopicCompletionStore>();
        completions.GetAsync(Arg.Any<Guid>(), topic.Id, Arg.Any<CancellationToken>())
            .Returns((Domain.Vocabulary.TopicCompletionRecord?)null);
        var learnerId = Guid.NewGuid();
        var vocabulary = EmptyVocabulary();

        var handler = new SubmitTopicQuizCommandHandler(
            _topics, completions, Enrollment(vocabulary), Substitute.For<IDailyProgressRecorder>(), TopicAccessTestDoubles.AllowAll(), new FixedTimeProvider(Now));
        var result = await handler.Handle(
            new SubmitTopicQuizCommand(topic.Id, answers, learnerId), CancellationToken.None);

        result.Completion.Should().NotBeNull();
        result.Completion!.Modules.Single(m => m.Module == "Vocabulary").Passed.Should().BeTrue();
        await completions.Received(1).SaveAsync(Arg.Any<Domain.Vocabulary.TopicCompletionRecord>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_saves_the_topic_words_to_the_learner_SRS_queue()
    {
        var topic = FilledTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var quiz = topic.BuildQuiz();
        var answers = quiz.ToDictionary(q => q.Index, q => q.CorrectOptionIndex);
        var vocabulary = EmptyVocabulary();
        var learnerId = Guid.NewGuid();

        var handler = new SubmitTopicQuizCommandHandler(
            _topics, EmptyCompletions(), Enrollment(vocabulary), Substitute.For<IDailyProgressRecorder>(), TopicAccessTestDoubles.AllowAll(), new FixedTimeProvider(Now));
        await handler.Handle(new SubmitTopicQuizCommand(topic.Id, answers, learnerId), CancellationToken.None);

        // Every target word is saved with a link back to its topic (for the review picture cards).
        await vocabulary.Received(topic.Words.Count).SaveAsync(
            Arg.Is<Domain.Vocabulary.VocabularyItem>(v =>
                v.LearnerId == learnerId && v.SourceTopicId == topic.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_saves_every_topic_word_even_when_the_same_spelling_exists_from_another_topic()
    {
        // Regression: dedup is per-topic, not global. A word already saved from a DIFFERENT topic
        // must not be dropped here, otherwise a topic lands 14 of its 15 words in "Mening so'zlarim".
        var topic = FilledTopic();
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var quiz = topic.BuildQuiz();
        var answers = quiz.ToDictionary(q => q.Index, q => q.CorrectOptionIndex);
        var learnerId = Guid.NewGuid();

        // The learner already has "reduce" - but from another topic, so it must still be saved here.
        var fromOtherTopic = Domain.Vocabulary.VocabularyItem.Learn(
            learnerId, "reduce", "kamaytirmoq", Now, sourceTopicId: Guid.NewGuid());
        var vocabulary = Substitute.For<IVocabularyRepository>();
        vocabulary.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[] { fromOtherTopic });

        var handler = new SubmitTopicQuizCommandHandler(
            _topics, EmptyCompletions(), Enrollment(vocabulary), Substitute.For<IDailyProgressRecorder>(), TopicAccessTestDoubles.AllowAll(), new FixedTimeProvider(Now));
        await handler.Handle(new SubmitTopicQuizCommand(topic.Id, answers, learnerId), CancellationToken.None);

        // All words (including "reduce") saved for THIS topic.
        await vocabulary.Received(topic.Words.Count).SaveAsync(
            Arg.Is<Domain.Vocabulary.VocabularyItem>(v => v.SourceTopicId == topic.Id),
            Arg.Any<CancellationToken>());
        await vocabulary.Received(1).SaveAsync(
            Arg.Is<Domain.Vocabulary.VocabularyItem>(v => v.SourceTopicId == topic.Id && v.Word == "reduce"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitQuiz_carries_the_part_of_speech_onto_the_saved_word()
    {
        var topic = PendingTopic();
        topic.FillContent("Passage about water.", new[]
        {
            TopicWord.Create("reduce", "kamaytirmoq", "We must reduce our use of water.", PartOfSpeech.Verb),
            TopicWord.Create("waste", "isrof qilmoq", "Do not waste clean water.", PartOfSpeech.Verb),
            TopicWord.Create("supply", "ta'minot", "The water supply is small.", PartOfSpeech.Noun),
            TopicWord.Create("clean", "toza", "Do not waste clean water.", PartOfSpeech.Adjective),
        });
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var quiz = topic.BuildQuiz();
        var answers = quiz.ToDictionary(q => q.Index, q => q.CorrectOptionIndex);
        var vocabulary = EmptyVocabulary();
        var learnerId = Guid.NewGuid();

        var handler = new SubmitTopicQuizCommandHandler(
            _topics, EmptyCompletions(), Enrollment(vocabulary), Substitute.For<IDailyProgressRecorder>(), TopicAccessTestDoubles.AllowAll(), new FixedTimeProvider(Now));
        await handler.Handle(new SubmitTopicQuizCommand(topic.Id, answers, learnerId), CancellationToken.None);

        await vocabulary.Received(1).SaveAsync(
            Arg.Is<Domain.Vocabulary.VocabularyItem>(v => v.Word == "supply" && v.PartOfSpeech == PartOfSpeech.Noun),
            Arg.Any<CancellationToken>());
        await vocabulary.Received(1).SaveAsync(
            Arg.Is<Domain.Vocabulary.VocabularyItem>(v => v.Word == "clean" && v.PartOfSpeech == PartOfSpeech.Adjective),
            Arg.Any<CancellationToken>());
    }

    /// <summary>A vocabulary repository where the learner has no saved words yet.</summary>
    private static IVocabularyRepository EmptyVocabulary()
    {
        var vocabulary = Substitute.For<IVocabularyRepository>();
        vocabulary.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Vocabulary.VocabularyItem>());
        return vocabulary;
    }

    private static ITopicVocabularyEnrollmentService Enrollment(IVocabularyRepository vocabulary) =>
        new TopicVocabularyEnrollmentService(vocabulary);

    [Fact]
    public async Task CheckPronunciation_reports_correct_for_a_good_score()
    {
        var assessor = Substitute.For<IPronunciationAssessor>();
        assessor.AssessAsync(Arg.Any<byte[]>(), "supply", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(95, 95, 95, 100,
                new[]
                {
                    new WordPronunciation("supply", 95, PronunciationErrorType.None,
                        new[]
                        {
                            new PhonemePronunciation("s", 98),
                            new PhonemePronunciation("ə", 92),
                            new PhonemePronunciation("p", 95),
                            new PhonemePronunciation("l", 94),
                            new PhonemePronunciation("aɪ", 96),
                        })
                }));
        var feedback = Substitute.For<IFeedbackTemplateProvider>();
        feedback.Get("pron.good", Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("Ajoyib!");

        var handler = new CheckWordPronunciationCommandHandler(assessor, feedback);
        var dto = await handler.Handle(
            new CheckWordPronunciationCommand("supply", new byte[] { 1, 2, 3 }), CancellationToken.None);

        dto.Recognized.Should().BeTrue();
        dto.Correct.Should().BeTrue();
        dto.IsAuthentic.Should().BeTrue();
        dto.Phonemes.Should().HaveCount(5);
        dto.Phonemes[1].Should().BeEquivalentTo(new { Phoneme = "ə", AccuracyScore = 92d });
        dto.FeedbackUz.Should().Be("Ajoyib!");
    }

    [Fact]
    public async Task CheckPronunciation_reports_incorrect_for_a_mispronunciation()
    {
        var assessor = Substitute.For<IPronunciationAssessor>();
        assessor.AssessAsync(Arg.Any<byte[]>(), "drought", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(45, 45, 60, 100,
                new[] { new WordPronunciation("drought", 45, PronunciationErrorType.Mispronunciation) }));
        var feedback = Substitute.For<IFeedbackTemplateProvider>();
        feedback.Get("pron.mispronunciation", Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns("Qayta urinib ko'ring.");

        var handler = new CheckWordPronunciationCommandHandler(assessor, feedback);
        var dto = await handler.Handle(
            new CheckWordPronunciationCommand("drought", new byte[] { 9 }), CancellationToken.None);

        dto.Correct.Should().BeFalse();
        dto.ErrorType.Should().Be(PronunciationErrorType.Mispronunciation);
        dto.FeedbackUz.Should().Be("Qayta urinib ko'ring.");
    }

    [Fact]
    public async Task CheckPronunciation_does_not_expose_local_simulation_as_a_real_score()
    {
        var assessor = Substitute.For<IPronunciationAssessor>();
        assessor.AssessAsync(Arg.Any<byte[]>(), "really", Arg.Any<CancellationToken>())
            .Returns(new PronunciationResult(58, 58, 61, 100,
                new[] { new WordPronunciation("really", 58, PronunciationErrorType.Mispronunciation) },
                isAuthentic: false));
        var feedback = Substitute.For<IFeedbackTemplateProvider>();
        feedback.Get("pron.not_recognized", Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("Real baholash mavjud emas.");

        var handler = new CheckWordPronunciationCommandHandler(assessor, feedback);
        var dto = await handler.Handle(
            new CheckWordPronunciationCommand("really", new byte[] { 1, 2, 3 }), CancellationToken.None);

        dto.Recognized.Should().BeFalse();
        dto.IsAuthentic.Should().BeFalse();
        dto.OverallScore.Should().Be(0);
        dto.Phonemes.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckPronunciation_reports_not_recognized_for_empty_audio()
    {
        var assessor = Substitute.For<IPronunciationAssessor>();
        var feedback = Substitute.For<IFeedbackTemplateProvider>();
        feedback.Get("pron.not_recognized", Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("Eshitilmadi.");

        var handler = new CheckWordPronunciationCommandHandler(assessor, feedback);
        var dto = await handler.Handle(
            new CheckWordPronunciationCommand("supply", Array.Empty<byte>()), CancellationToken.None);

        dto.Recognized.Should().BeFalse();
        dto.Correct.Should().BeFalse();
        await assessor.DidNotReceive().AssessAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
