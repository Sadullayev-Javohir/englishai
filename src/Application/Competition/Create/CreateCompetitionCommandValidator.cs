using FluentValidation;

namespace Application.Competition.Create;

public sealed class CreateCompetitionCommandValidator
    : AbstractValidator<CreateCompetitionCommand>
{
    public CreateCompetitionCommandValidator()
    {
        RuleFor(c => c.HostLearnerId)
            .NotEmpty().WithMessage("Host learner id must not be empty.");

        RuleFor(c => c.HostDisplayName)
            .NotEmpty().WithMessage("Host display name must not be empty.")
            .MaximumLength(60).WithMessage("Host display name is too long.");

        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("Competition title must not be empty.")
            .MaximumLength(120).WithMessage("Competition title is too long.");

        RuleFor(c => c.TopicIds)
            .NotNull().WithMessage("Topic ids must not be null.")
            .Must(ids => ids.Count >= 1).WithMessage("A competition needs at least one topic.");

        RuleFor(c => c.Settings)
            .NotNull().WithMessage("Competition settings must not be null.");
    }
}
