using Application.Assessment.FinalizePlacementTest;
using Application.Assessment.ResumePlacementTest;
using Application.Assessment.StartPlacementTest;
using Application.Assessment.SubmitAnswer;
using Application.Assessment.SubmitSpeaking;
using Application.Assessment.SubmitWriting;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Assessment;

public class ValidatorsTests
{
    [Fact]
    public void Start_validator_rejects_empty_learner_id()
    {
        var result = new StartPlacementTestCommandValidator()
            .TestValidate(new StartPlacementTestCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void Start_validator_accepts_valid_learner_id()
    {
        var result = new StartPlacementTestCommandValidator()
            .TestValidate(new StartPlacementTestCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Submit_validator_rejects_negative_option_index()
    {
        var result = new SubmitAnswerCommandValidator()
            .TestValidate(new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), -1));

        result.ShouldHaveValidationErrorFor(x => x.SelectedOptionIndex);
    }

    [Fact]
    public void Submit_validator_rejects_empty_ids()
    {
        var result = new SubmitAnswerCommandValidator()
            .TestValidate(new SubmitAnswerCommand(Guid.Empty, Guid.Empty, 0));

        result.ShouldHaveValidationErrorFor(x => x.SessionId);
        result.ShouldHaveValidationErrorFor(x => x.QuestionId);
    }

    [Fact]
    public void Finalize_validator_rejects_empty_session_id()
    {
        var result = new FinalizePlacementTestCommandValidator()
            .TestValidate(new FinalizePlacementTestCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public void Writing_validator_requires_session_task_and_text()
    {
        var result = new SubmitWritingCommandValidator()
            .TestValidate(new SubmitWritingCommand(Guid.Empty, Guid.Empty, ""));

        result.ShouldHaveValidationErrorFor(x => x.SessionId);
        result.ShouldHaveValidationErrorFor(x => x.TaskId);
        result.ShouldHaveValidationErrorFor(x => x.Text);
    }

    [Fact]
    public void Speaking_validator_rejects_short_audio_and_empty_task()
    {
        var result = new SubmitSpeakingCommandValidator()
            .TestValidate(new SubmitSpeakingCommand(Guid.NewGuid(), Guid.Empty, new byte[100]));

        result.ShouldHaveValidationErrorFor(x => x.TaskId);
        result.ShouldHaveValidationErrorFor(x => x.AudioContent);
    }

    [Fact]
    public void Speaking_validator_rejects_null_audio_without_throwing()
    {
        new SubmitSpeakingCommandValidator()
            .TestValidate(new SubmitSpeakingCommand(Guid.NewGuid(), Guid.NewGuid(), null!))
            .ShouldHaveValidationErrorFor(x => x.AudioContent);
    }

    [Fact]
    public void Speaking_validator_rejects_oversized_recordings()
    {
        new SubmitSpeakingCommandValidator()
            .TestValidate(new SubmitSpeakingCommand(Guid.NewGuid(), Guid.NewGuid(), new byte[4_000_001]))
            .ShouldHaveValidationErrorFor(x => x.AudioContent);
    }

    [Fact]
    public void Resume_validator_rejects_empty_session_id()
    {
        var result = new ResumePlacementTestQueryValidator()
            .TestValidate(new ResumePlacementTestQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }
}
