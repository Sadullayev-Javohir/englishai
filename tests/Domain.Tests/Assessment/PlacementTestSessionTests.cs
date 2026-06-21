using Domain.Assessment;
using Domain.Common;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Domain.Tests.Assessment;

public class PlacementTestSessionTests
{
    private static void Answer(PlacementTestSession session, int count, bool correct)
    {
        for (var i = 0; i < count; i++)
        {
            var itemId = Guid.NewGuid();
            session.ServeItem(itemId);
            session.RecordAnswer(itemId, correct);
        }
    }

    /// <summary>Fills the current stage to its quota - multiple-choice stages with
    /// <see cref="PlacementTestSession.RecordAnswer"/>, productive stages (Writing,
    /// Speaking) with a single <see cref="PlacementTestSession.RecordProductiveResult"/>.</summary>
    private static void CompleteCurrentStage(PlacementTestSession session, bool correct, int productiveScore = 80)
    {
        if (session.IsCurrentStageProductive)
        {
            var taskId = Guid.NewGuid();
            session.ServeItem(taskId);
            session.RecordProductiveResult(taskId, productiveScore, session.CurrentDifficulty);
            return;
        }

        var quota = PlacementTestSession.QuestionsPerStage[session.CurrentStage];
        Answer(session, quota, correct);
    }

    [Fact]
    public void Start_begins_at_vocabulary_stage_and_default_difficulty()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());

        session.CurrentStage.Should().Be(TestStage.Vocabulary);
        session.CurrentDifficulty.Should().Be(PlacementTestSession.DefaultStartingDifficulty);
        session.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Start_rejects_empty_learner_id()
    {
        var act = () => PlacementTestSession.Start(Guid.Empty);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Three_consecutive_correct_steps_difficulty_up()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid()); // starts A2

        Answer(session, 3, correct: true);

        session.CurrentDifficulty.Should().Be(CefrLevel.B1);
    }

    [Fact]
    public void Two_consecutive_wrong_steps_difficulty_down()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid()); // starts A2

        Answer(session, 2, correct: false);

        session.CurrentDifficulty.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Streak_counter_resets_after_a_level_change()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid()); // A2

        Answer(session, 3, correct: true);  // -> B1, counter reset
        Answer(session, 2, correct: true);  // only 2, not enough to step again

        session.CurrentDifficulty.Should().Be(CefrLevel.B1);
    }

    [Fact]
    public void A_wrong_answer_breaks_a_correct_streak()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid()); // A2

        Answer(session, 2, correct: true);
        Answer(session, 1, correct: false); // breaks streak, only 1 wrong so no step down
        Answer(session, 2, correct: true);  // 2 correct again, still not 3 in a row

        session.CurrentDifficulty.Should().Be(CefrLevel.A2);
    }

    [Fact]
    public void Difficulty_does_not_rise_above_C2()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), startingDifficulty: CefrLevel.C2);

        Answer(session, 3, correct: true);

        session.CurrentDifficulty.Should().Be(CefrLevel.C2);
    }

    [Fact]
    public void Difficulty_does_not_fall_below_A1()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), startingDifficulty: CefrLevel.A1);

        Answer(session, 2, correct: false);

        session.CurrentDifficulty.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Stage_advances_after_its_question_quota_is_reached()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        var quota = PlacementTestSession.QuestionsPerStage[TestStage.Vocabulary];

        Answer(session, quota, correct: true);

        session.CurrentStage.Should().Be(TestStage.Grammar);
        session.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Test_completes_after_the_last_stage_when_speaking_excluded()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), includeSpeaking: false);

        CompleteCurrentStage(session, correct: true); // Vocabulary
        CompleteCurrentStage(session, correct: true); // Grammar
        CompleteCurrentStage(session, correct: true); // Listening
        CompleteCurrentStage(session, correct: true); // Reading
        CompleteCurrentStage(session, correct: true); // Writing

        session.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Recording_an_answer_after_completion_throws()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), includeSpeaking: false);
        while (!session.IsCompleted)
            CompleteCurrentStage(session, correct: true);

        var act = () => session.RecordAnswer(Guid.NewGuid(), true);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finalize_before_completion_throws()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());

        var act = () => session.Finalize();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Finalize_after_completion_returns_a_result_with_stage_breakdown()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), includeSpeaking: false);
        CompleteCurrentStage(session, correct: true); // Vocabulary
        CompleteCurrentStage(session, correct: true); // Grammar
        CompleteCurrentStage(session, correct: true); // Listening
        CompleteCurrentStage(session, correct: true); // Reading
        CompleteCurrentStage(session, correct: true); // Writing

        var result = session.Finalize();

        result.StageResults.Should().ContainKeys(
            TestStage.Vocabulary, TestStage.Grammar, TestStage.Listening,
            TestStage.Reading, TestStage.Writing);
        result.StageResults.Should().NotContainKey(TestStage.Speaking);
    }

    [Fact]
    public void Start_exposes_six_stage_twenty_three_item_progress()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());

        session.StageCount.Should().Be(6);
        session.TotalItemCount.Should().Be(23);
        session.CurrentStageNumber.Should().Be(1);
        session.CompletedItemCount.Should().Be(0);
    }

    [Fact]
    public void Submission_must_match_the_current_served_item()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        var served = Guid.NewGuid();
        session.ServeItem(served);

        var act = () => session.RecordAnswer(Guid.NewGuid(), isCorrect: true);

        act.Should().Throw<DomainException>();
        session.TotalQuestionsAnswered.Should().Be(0);
    }

    [Fact]
    public void Productive_result_sets_the_next_stage_starting_difficulty()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        CompleteCurrentStage(session, correct: true); // Vocabulary
        CompleteCurrentStage(session, correct: true); // Grammar
        CompleteCurrentStage(session, correct: true); // Listening
        CompleteCurrentStage(session, correct: true); // Reading
        var writingTaskId = Guid.NewGuid();
        session.ServeItem(writingTaskId);

        session.RecordProductiveResult(writingTaskId, 15, session.CurrentDifficulty);

        session.CurrentStage.Should().Be(TestStage.Speaking);
        session.CurrentDifficulty.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Snapshot_round_trip_preserves_position_difficulty_and_answers()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid()); // A2, Vocabulary
        Answer(session, 3, correct: true);  // -> B1, streak reset
        Answer(session, 2, correct: false); // mid-stage, one wrong on the streak

        var restored = PlacementTestSession.Restore(session.ToSnapshot());

        restored.Id.Should().Be(session.Id);
        restored.LearnerId.Should().Be(session.LearnerId);
        restored.CurrentStage.Should().Be(session.CurrentStage);
        restored.CurrentDifficulty.Should().Be(session.CurrentDifficulty);
        restored.StartingDifficulty.Should().Be(session.StartingDifficulty);
        restored.IsCompleted.Should().Be(session.IsCompleted);
        restored.TotalQuestionsAnswered.Should().Be(session.TotalQuestionsAnswered);
    }

    [Theory]
    [InlineData(CefrLevel.A1)]
    [InlineData(CefrLevel.A2)]
    [InlineData(CefrLevel.B1)]
    [InlineData(CefrLevel.B2)]
    [InlineData(CefrLevel.C1)]
    public void Snapshot_round_trip_preserves_the_level_being_tested(CefrLevel testedLevel)
    {
        var session = PlacementTestSession.Start(
            Guid.NewGuid(),
            includeSpeaking: true,
            startingDifficulty: testedLevel);
        Answer(session, 3, correct: true);

        var json = JsonSerializer.Serialize(session.ToSnapshot());
        var snapshot = JsonSerializer.Deserialize<PlacementSessionSnapshot>(json);
        var restored = PlacementTestSession.Restore(snapshot!);

        restored.StartingDifficulty.Should().Be(testedLevel);
        restored.CurrentDifficulty.Should().Be(session.CurrentDifficulty);
    }

    [Fact]
    public void Legacy_snapshot_without_starting_difficulty_can_still_be_restored()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), startingDifficulty: CefrLevel.B1);
        Answer(session, 3, correct: true);
        var legacyJson = JsonSerializer.Serialize(session.ToSnapshot())
            .Replace(",\"StartingDifficulty\":3", string.Empty, StringComparison.Ordinal);

        var snapshot = JsonSerializer.Deserialize<PlacementSessionSnapshot>(legacyJson);
        var restored = PlacementTestSession.Restore(snapshot!);

        restored.StartingDifficulty.Should().Be(restored.CurrentDifficulty);
    }

    [Fact]
    public void Restored_session_continues_the_adaptive_streak_exactly()
    {
        // A learner one wrong answer away from stepping down should still step down after a
        // restore - the streak counters must survive serialization, not reset to zero.
        var session = PlacementTestSession.Start(Guid.NewGuid()); // A2
        Answer(session, 1, correct: false); // one wrong; one more would step down to A1

        var restored = PlacementTestSession.Restore(session.ToSnapshot());
        var nextItemId = Guid.NewGuid();
        restored.ServeItem(nextItemId);
        restored.RecordAnswer(nextItemId, isCorrect: false); // the second consecutive wrong

        restored.CurrentDifficulty.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Restored_completed_session_finalizes_to_the_same_result()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), includeSpeaking: false);
        CompleteCurrentStage(session, correct: true); // Vocabulary
        CompleteCurrentStage(session, correct: true); // Grammar
        CompleteCurrentStage(session, correct: true); // Listening
        CompleteCurrentStage(session, correct: true); // Reading
        CompleteCurrentStage(session, correct: true); // Writing
        session.IsCompleted.Should().BeTrue();
        session.MarkExitTestFinalized(CefrLevel.B1);

        var restored = PlacementTestSession.Restore(session.ToSnapshot());

        restored.IsCompleted.Should().BeTrue();
        restored.FinalizedLevel.Should().Be(CefrLevel.B1);
        restored.Finalize().OverallLevel.Should().Be(session.Finalize().OverallLevel);
    }

    [Fact]
    public void Integrity_incidents_are_idempotent_and_repeats_do_not_add_up()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        var firstIncident = Guid.NewGuid();

        session.RecordIntegrityViolation(firstIncident).Should().BeTrue();
        session.RecordIntegrityViolation(firstIncident).Should().BeFalse();
        session.IntegrityViolationCount.Should().Be(1);
        session.IsIntegrityInvalidated.Should().BeFalse();
    }

    [Fact]
    public void A_session_survives_every_incident_below_the_threshold()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());

        for (var i = 1; i < PlacementTestSession.ViolationsBeforeInvalidation; i++)
        {
            session.RecordIntegrityViolation(Guid.NewGuid()).Should().BeTrue();
            session.IsIntegrityInvalidated.Should().BeFalse(
                "a phone raises these signals for keyboards and permission prompts too");
        }

        session.RecordIntegrityViolation(Guid.NewGuid()).Should().BeTrue();
        session.IntegrityViolationCount.Should().Be(PlacementTestSession.ViolationsBeforeInvalidation);
        session.IsIntegrityInvalidated.Should().BeTrue();
    }

    [Fact]
    public void Invalidated_session_rejects_test_operations()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        for (var i = 0; i < PlacementTestSession.ViolationsBeforeInvalidation; i++)
            session.RecordIntegrityViolation(Guid.NewGuid());

        var act = () => session.ServeItem(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Snapshot_round_trip_preserves_integrity_state_and_incident_ids()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        var firstIncident = Guid.NewGuid();
        session.RecordIntegrityViolation(firstIncident);

        var restored = PlacementTestSession.Restore(session.ToSnapshot());

        restored.IntegrityViolationCount.Should().Be(1);
        restored.IsIntegrityInvalidated.Should().BeFalse();
        restored.RecordIntegrityViolation(firstIncident).Should().BeFalse();
        for (var i = 1; i < PlacementTestSession.ViolationsBeforeInvalidation; i++)
            restored.RecordIntegrityViolation(Guid.NewGuid()).Should().BeTrue();
        restored.IsIntegrityInvalidated.Should().BeTrue();
    }

    [Fact]
    public void Legacy_snapshot_without_integrity_fields_restores_as_clean()
    {
        var json = JsonSerializer.Serialize(PlacementTestSession.Start(Guid.NewGuid()).ToSnapshot())
            .Replace(",\"IntegrityViolationCount\":0", string.Empty, StringComparison.Ordinal)
            .Replace(",\"IsIntegrityInvalidated\":false", string.Empty, StringComparison.Ordinal)
            .Replace(",\"IntegrityIncidentIds\":[]", string.Empty, StringComparison.Ordinal);

        var restored = PlacementTestSession.Restore(JsonSerializer.Deserialize<PlacementSessionSnapshot>(json)!);

        restored.IntegrityViolationCount.Should().Be(0);
        restored.IsIntegrityInvalidated.Should().BeFalse();
    }
}
