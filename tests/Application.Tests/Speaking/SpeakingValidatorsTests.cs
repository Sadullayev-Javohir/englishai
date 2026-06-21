using Application.Speaking.AccentTutors;
using Application.Speaking.AssessSegmentPronunciation;
using Application.Speaking.EvaluateRoleplay;
using Application.Speaking.GetIdeaCards;
using Application.Speaking.GetWordPronunciationDetail;
using Application.Speaking.StartConversation;
using Application.Speaking.StartRoleplay;
using Application.Speaking.SubmitUtterance;
using Domain.Assessment;
using Domain.Speaking;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Speaking;

public class SpeakingValidatorsTests
{
    [Fact]
    public void Start_validator_rejects_empty_learner_id()
    {
        new StartConversationCommandValidator()
            .TestValidate(new StartConversationCommand(Guid.Empty, CefrLevel.A1))
            .ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void Start_validator_accepts_a_short_topic()
    {
        new StartConversationCommandValidator()
            .TestValidate(new StartConversationCommand(Guid.NewGuid(), CefrLevel.A1, "travel"))
            .ShouldNotHaveValidationErrorFor(x => x.Topic);
    }

    [Fact]
    public void Start_validator_rejects_an_overlong_topic()
    {
        new StartConversationCommandValidator()
            .TestValidate(new StartConversationCommand(Guid.NewGuid(), CefrLevel.A1, new string('x', 41)))
            .ShouldHaveValidationErrorFor(x => x.Topic);
    }

    [Fact]
    public void IdeaCards_validator_rejects_empty_session_id()
    {
        new GetIdeaCardsQueryValidator()
            .TestValidate(new GetIdeaCardsQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public void IdeaCards_validator_accepts_a_session_id()
    {
        new GetIdeaCardsQueryValidator()
            .TestValidate(new GetIdeaCardsQuery(Guid.NewGuid()))
            .ShouldNotHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public void Submit_validator_rejects_empty_audio()
    {
        new SubmitUtteranceCommandValidator()
            .TestValidate(new SubmitUtteranceCommand(Guid.NewGuid(), Array.Empty<byte>()))
            .ShouldHaveValidationErrorFor(x => x.AudioContent);
    }

    [Fact]
    public void Submit_validator_accepts_valid_command()
    {
        new SubmitUtteranceCommandValidator()
            .TestValidate(new SubmitUtteranceCommand(Guid.NewGuid(), new byte[] { 1 }))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Detail_validator_rejects_empty_word()
    {
        new GetWordPronunciationDetailQueryValidator()
            .TestValidate(new GetWordPronunciationDetailQuery(""))
            .ShouldHaveValidationErrorFor(x => x.Word);
    }

    [Fact]
    public void StartRoleplay_validator_rejects_empty_learner_id()
    {
        new StartRoleplayCommandValidator()
            .TestValidate(new StartRoleplayCommand(Guid.Empty, CefrLevel.A1, "restaurant"))
            .ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void StartRoleplay_validator_rejects_an_unknown_scenario_code()
    {
        new StartRoleplayCommandValidator()
            .TestValidate(new StartRoleplayCommand(Guid.NewGuid(), CefrLevel.A1, "no_such_scene"))
            .ShouldHaveValidationErrorFor(x => x.ScenarioCode);
    }

    [Fact]
    public void StartRoleplay_validator_rejects_an_empty_scenario_code()
    {
        new StartRoleplayCommandValidator()
            .TestValidate(new StartRoleplayCommand(Guid.NewGuid(), CefrLevel.A1, ""))
            .ShouldHaveValidationErrorFor(x => x.ScenarioCode);
    }

    [Fact]
    public void StartRoleplay_validator_accepts_a_valid_command()
    {
        new StartRoleplayCommandValidator()
            .TestValidate(new StartRoleplayCommand(Guid.NewGuid(), CefrLevel.A2, "job_interview"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EvaluateRoleplay_validator_rejects_empty_session_id()
    {
        new EvaluateRoleplayCommandValidator()
            .TestValidate(new EvaluateRoleplayCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public void AssessSegmentPronunciation_validator_rejects_empty_reference_text()
    {
        new AssessSegmentPronunciationCommandValidator()
            .TestValidate(new AssessSegmentPronunciationCommand("", new byte[] { 1 }))
            .ShouldHaveValidationErrorFor(x => x.ReferenceText);
    }

    [Fact]
    public void AssessSegmentPronunciation_validator_rejects_overlong_reference_text()
    {
        new AssessSegmentPronunciationCommandValidator()
            .TestValidate(new AssessSegmentPronunciationCommand(
                new string('a', AssessSegmentPronunciationCommandValidator.MaxReferenceLength + 1), new byte[] { 1 }))
            .ShouldHaveValidationErrorFor(x => x.ReferenceText);
    }

    [Fact]
    public void AssessSegmentPronunciation_validator_accepts_a_valid_command()
    {
        new AssessSegmentPronunciationCommandValidator()
            .TestValidate(new AssessSegmentPronunciationCommand("Hello there.", new byte[] { 1 }))
            .ShouldNotHaveAnyValidationErrors();
    }

    // A completion report is the only place a client hands the server a number that turns into
    // money, so the validator is the first line of defence against a malformed or hostile one.
    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")] // right length, not hex
    public void CompleteVoiceLiveSession_validator_rejects_malformed_session_ids(string sessionId)
    {
        new CompleteAccentTutorVoiceLiveSessionCommandValidator()
            .TestValidate(new CompleteAccentTutorVoiceLiveSessionCommand("british", sessionId, 30))
            .ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CompleteVoiceLiveSession_validator_rejects_impossible_durations(double seconds)
    {
        new CompleteAccentTutorVoiceLiveSessionCommandValidator()
            .TestValidate(new CompleteAccentTutorVoiceLiveSessionCommand("british", ValidSessionId, seconds))
            .ShouldHaveValidationErrorFor(x => x.DurationSeconds);
    }

    [Fact]
    public void CompleteVoiceLiveSession_validator_rejects_an_unknown_tutor()
    {
        new CompleteAccentTutorVoiceLiveSessionCommandValidator()
            .TestValidate(new CompleteAccentTutorVoiceLiveSessionCommand("klingon", ValidSessionId, 30))
            .ShouldHaveValidationErrorFor(x => x.TutorId);
    }

    [Fact]
    public void CompleteVoiceLiveSession_validator_accepts_a_valid_report()
    {
        new CompleteAccentTutorVoiceLiveSessionCommandValidator()
            .TestValidate(new CompleteAccentTutorVoiceLiveSessionCommand("british", ValidSessionId, 42.5))
            .ShouldNotHaveAnyValidationErrors();
    }

    private const string ValidSessionId = "0123456789abcdef0123456789abcdef";
}
