using FluentValidation;

namespace Application.Vocabulary.GetTopicCompletion;

public sealed class GetTopicCompletionQueryValidator : AbstractValidator<GetTopicCompletionQuery>
{
    public GetTopicCompletionQueryValidator()
    {
        RuleFor(q => q.LearnerId).NotEmpty();
        RuleFor(q => q.TopicId).NotEmpty();
    }
}
