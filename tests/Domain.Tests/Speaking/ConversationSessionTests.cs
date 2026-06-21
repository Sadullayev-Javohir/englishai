using Domain.Assessment;
using Domain.Common;
using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Speaking;

public class ConversationSessionTests
{
    [Fact]
    public void Curriculum_context_survives_snapshot_round_trip()
    {
        var context = new SpeakingCurriculumContext(
            Guid.NewGuid(), "My family", "Describe family members", "present-simple", null,
            new[] { "parents", "sibling" }, new[] { "Who lives with you?" });
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A1, curriculumContext: context);

        var restored = ConversationSession.Restore(session.ToSnapshot());

        restored.CurriculumContext.Should().BeEquivalentTo(context);
    }

    [Fact]
    public void Start_creates_an_empty_session_at_the_given_level()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1);

        session.Id.Should().NotBeEmpty();
        session.Level.Should().Be(CefrLevel.B1);
        session.Turns.Should().BeEmpty();
    }

    [Fact]
    public void Start_rejects_empty_learner_id()
    {
        var act = () => ConversationSession.Start(Guid.Empty, CefrLevel.A1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Start_stores_the_optional_topic()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, "travel");

        session.Topic.Should().Be("travel");
    }

    [Fact]
    public void Start_normalizes_blank_topic_to_null()
    {
        ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1).Topic.Should().BeNull();
        ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, "   ").Topic.Should().BeNull();
        ConversationSession.Start(Guid.NewGuid(), CefrLevel.B1, " travel ").Topic.Should().Be("travel");
    }

    [Fact]
    public void Start_keeps_focus_words_and_drops_blanks()
    {
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, "A Busy Morning",
            new[] { " hurry ", "", "  ", "breakfast" });

        session.FocusWords.Should().Equal("hurry", "breakfast");
    }

    [Fact]
    public void Start_defaults_focus_words_to_empty()
    {
        ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2).FocusWords.Should().BeEmpty();
    }

    [Fact]
    public void Turns_are_recorded_in_order_with_their_roles()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);

        session.AddTutorTurn("Hello! How are you today?");
        var pronunciation = new PronunciationResult(85, 85, 80, 100,
            new[] { new WordPronunciation("good", 85, PronunciationErrorType.None) });
        session.AddLearnerTurn("I am good, thank you.", pronunciation);

        session.Turns.Should().HaveCount(2);
        session.Turns[0].Role.Should().Be(ConversationRole.Tutor);
        session.Turns[1].Role.Should().Be(ConversationRole.Learner);
        session.LastTurn!.Pronunciation!.Band.Should().Be(PronunciationBand.Good);
    }

    [Fact]
    public void Learner_turn_rejects_empty_text()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var act = () => session.AddLearnerTurn("   ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Start_stores_the_optional_vocabulary_topic_id()
    {
        var topicId = Guid.NewGuid();
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, vocabularyTopicId: topicId);

        session.VocabularyTopicId.Should().Be(topicId);
        ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2).VocabularyTopicId.Should().BeNull();
        ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2, vocabularyTopicId: Guid.Empty)
            .VocabularyTopicId.Should().BeNull();
    }

    [Fact]
    public void First_spoken_activity_only_sets_the_baseline()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var t0 = new DateTimeOffset(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

        session.RecordSpokenActivity(t0).Should().Be(TimeSpan.Zero);
        session.SpokenTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Spoken_activity_accumulates_the_gap_between_utterances()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var t0 = new DateTimeOffset(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

        session.RecordSpokenActivity(t0);
        session.RecordSpokenActivity(t0.AddSeconds(40)).Should().Be(TimeSpan.FromSeconds(40));
        session.RecordSpokenActivity(t0.AddSeconds(100)).Should().Be(TimeSpan.FromSeconds(60));
        session.SpokenTime.Should().Be(TimeSpan.FromSeconds(100));
    }

    [Fact]
    public void Long_idle_gap_between_utterances_is_capped()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var t0 = new DateTimeOffset(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

        session.RecordSpokenActivity(t0);
        // A 30-minute pause counts only up to the cap, so idle time does not inflate the total.
        session.RecordSpokenActivity(t0.AddMinutes(30)).Should().Be(ConversationSession.MaxCountedGap);
        session.SpokenTime.Should().Be(ConversationSession.MaxCountedGap);
    }

    [Fact]
    public void Session_reaches_the_speaking_limit_once_engaged_time_hits_the_cap()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2);
        var t0 = new DateTimeOffset(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

        session.HasReachedSpeakingLimit.Should().BeFalse();

        // Accumulate engaged-speaking time in capped steps up to the 10-minute cap.
        session.RecordSpokenActivity(t0);
        var elapsed = TimeSpan.Zero;
        var step = ConversationSession.MaxCountedGap;
        while (session.SpokenTime < ConversationSession.MaxSpokenTime)
        {
            elapsed += step;
            session.RecordSpokenActivity(t0 + elapsed);
        }

        session.SpokenTime.Should().BeGreaterThanOrEqualTo(ConversationSession.MaxSpokenTime);
        session.HasReachedSpeakingLimit.Should().BeTrue();
    }
}
