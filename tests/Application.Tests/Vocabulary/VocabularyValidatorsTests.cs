using Application.Vocabulary.GetMandatoryReviewStatus;
using Application.Vocabulary.LearnWord;
using Application.Vocabulary.SubmitReview;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Vocabulary;

public class VocabularyValidatorsTests
{
    [Fact]
    public void LearnWord_validator_rejects_empty_word()
    {
        new LearnWordCommandValidator()
            .TestValidate(new LearnWordCommand(Guid.NewGuid(), "", "yangilik"))
            .ShouldHaveValidationErrorFor(x => x.Word);
    }

    [Fact]
    public void LearnWord_validator_rejects_empty_translation()
    {
        new LearnWordCommandValidator()
            .TestValidate(new LearnWordCommand(Guid.NewGuid(), "innovation", ""))
            .ShouldHaveValidationErrorFor(x => x.Translation);
    }

    [Fact]
    public void LearnWord_validator_accepts_valid_command()
    {
        new LearnWordCommandValidator()
            .TestValidate(new LearnWordCommand(Guid.NewGuid(), "innovation", "yangilik"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MandatoryReviewStatus_validator_rejects_empty_learner_id()
    {
        new GetMandatoryReviewStatusQueryValidator()
            .TestValidate(new GetMandatoryReviewStatusQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void SubmitReview_validator_rejects_empty_item_id()
    {
        new SubmitReviewCommandValidator()
            .TestValidate(new SubmitReviewCommand(Guid.Empty, SelfRatedPassed: true))
            .ShouldHaveValidationErrorFor(x => x.VocabularyItemId);
    }

    [Fact]
    public void SubmitReview_validator_rejects_neither_answer_nor_self_rating()
    {
        var result = new SubmitReviewCommandValidator().Validate(new SubmitReviewCommand(Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SubmitReview_validator_accepts_a_submitted_answer()
    {
        new SubmitReviewCommandValidator()
            .TestValidate(new SubmitReviewCommand(Guid.NewGuid(), SubmittedAnswer: "innovation"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SubmitReview_validator_accepts_a_self_rating()
    {
        new SubmitReviewCommandValidator()
            .TestValidate(new SubmitReviewCommand(Guid.NewGuid(), SelfRatedPassed: false))
            .ShouldNotHaveAnyValidationErrors();
    }
}
