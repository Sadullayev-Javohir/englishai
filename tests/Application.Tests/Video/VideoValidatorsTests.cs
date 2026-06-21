using Application.Video.Dtos;
using Application.Video.ExplainSegment;
using Application.Video.GetVideoCatalog;
using Application.Video.IngestVideo;
using Application.Video.RateVideoDifficulty;
using Application.Video.SubmitVideoQuiz;
using Domain.Video;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Video;

public class VideoValidatorsTests
{
    [Fact]
    public void IngestVideo_validator_rejects_empty_video_id()
    {
        new IngestVideoCommandValidator()
            .TestValidate(new IngestVideoCommand("", "technology"))
            .ShouldHaveValidationErrorFor(x => x.YouTubeVideoId);
    }

    [Fact]
    public void IngestVideo_validator_rejects_empty_topic()
    {
        new IngestVideoCommandValidator()
            .TestValidate(new IngestVideoCommand("abc123", ""))
            .ShouldHaveValidationErrorFor(x => x.Topic);
    }

    [Fact]
    public void GetVideoCatalog_validator_rejects_empty_learner()
    {
        new GetVideoCatalogQueryValidator()
            .TestValidate(new GetVideoCatalogQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void SubmitVideoQuiz_validator_rejects_empty_answers()
    {
        new SubmitVideoQuizCommandValidator()
            .TestValidate(new SubmitVideoQuizCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<QuizAnswer>()))
            .ShouldHaveValidationErrorFor(x => x.Answers);
    }

    [Fact]
    public void SubmitVideoQuiz_validator_rejects_negative_option_index()
    {
        new SubmitVideoQuizCommandValidator()
            .TestValidate(new SubmitVideoQuizCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { new QuizAnswer(Guid.NewGuid(), -1) }))
            .ShouldHaveValidationErrorFor("Answers[0].SelectedOptionIndex");
    }

    [Fact]
    public void RateVideoDifficulty_validator_accepts_valid_command()
    {
        new RateVideoDifficultyCommandValidator()
            .TestValidate(new RateVideoDifficultyCommand(Guid.NewGuid(), Guid.NewGuid(), VideoDifficultyRating.JustRight))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ExplainSegment_validator_rejects_empty_video_lesson_id()
    {
        new ExplainSegmentQueryValidator()
            .TestValidate(new ExplainSegmentQuery(Guid.Empty, "A sentence.", "Why this word?", Array.Empty<ChatTurnDto>()))
            .ShouldHaveValidationErrorFor(x => x.VideoLessonId);
    }

    [Fact]
    public void ExplainSegment_validator_rejects_focus_over_max_length()
    {
        new ExplainSegmentQueryValidator()
            .TestValidate(new ExplainSegmentQuery(
                Guid.NewGuid(), new string('a', ExplainSegmentQueryValidator.MaxFocusLength + 1),
                "Why this word?", Array.Empty<ChatTurnDto>()))
            .ShouldHaveValidationErrorFor(x => x.FocusText);
    }

    [Fact]
    public void ExplainSegment_validator_rejects_empty_user_message()
    {
        new ExplainSegmentQueryValidator()
            .TestValidate(new ExplainSegmentQuery(Guid.NewGuid(), "A sentence.", "", Array.Empty<ChatTurnDto>()))
            .ShouldHaveValidationErrorFor(x => x.UserMessage);
    }

    [Fact]
    public void ExplainSegment_validator_rejects_message_over_max_length()
    {
        new ExplainSegmentQueryValidator()
            .TestValidate(new ExplainSegmentQuery(
                Guid.NewGuid(), "A sentence.", new string('a', ExplainSegmentQueryValidator.MaxMessageLength + 1),
                Array.Empty<ChatTurnDto>()))
            .ShouldHaveValidationErrorFor(x => x.UserMessage);
    }

    [Fact]
    public void ExplainSegment_validator_accepts_valid_query()
    {
        new ExplainSegmentQueryValidator()
            .TestValidate(new ExplainSegmentQuery(
                Guid.NewGuid(), "A sentence.", "Why this word?", Array.Empty<ChatTurnDto>()))
            .ShouldNotHaveAnyValidationErrors();
    }
}
