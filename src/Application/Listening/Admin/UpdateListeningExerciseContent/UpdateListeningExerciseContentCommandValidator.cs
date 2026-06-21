using Domain.Assessment;
using Domain.Listening;
using FluentValidation;

namespace Application.Listening.Admin.UpdateListeningExerciseContent;

public sealed class UpdateListeningExerciseContentCommandValidator
    : AbstractValidator<UpdateListeningExerciseContentCommand>
{
    public UpdateListeningExerciseContentCommandValidator()
    {
        RuleFor(x => x.Payload.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Payload.Topic).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Payload.Level).Must(value => Enum.TryParse<CefrLevel>(value, true, out _));
        RuleFor(x => x.Payload.Status).Must(value => Enum.TryParse<ListeningExerciseStatus>(value, true, out _));
        RuleFor(x => x.Payload.Transcript).MaximumLength(4000);
        RuleForEach(x => x.Payload.Segments).ChildRules(segment =>
        {
            segment.RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
            segment.RuleFor(x => x.StartMs).GreaterThanOrEqualTo(0);
            segment.RuleFor(x => x).Must(x => x.EndMs > x.StartMs).WithMessage("Segment end must be after start.");
            segment.RuleFor(x => x.Speaker).NotEmpty().MaximumLength(100);
            segment.RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        });
        RuleForEach(x => x.Payload.Questions).ChildRules(question =>
        {
            question.RuleFor(x => x.Prompt).NotEmpty().MaximumLength(2000);
            question.RuleFor(x => x.Options).NotNull().Must(options => options.Count >= ListeningQuestion.MinOptions);
            question.RuleFor(x => x).Must(x => x.CorrectOptionIndex >= 0 && x.CorrectOptionIndex < x.Options.Count)
                .WithMessage("Correct option index is out of range.");
        });
        When(x => string.Equals(x.Payload.Status, nameof(ListeningExerciseStatus.Filled), StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.Payload.Transcript).NotEmpty();
            RuleFor(x => x.Payload.Questions).NotEmpty();
        });
    }
}
